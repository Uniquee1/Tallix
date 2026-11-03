using System.Text.Json;
using System.Text.Json.Serialization;
using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Users;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Sync;

public static class SyncStatus
{
    public const string Applied = "APPLIED";
    public const string AlreadyApplied = "ALREADY_APPLIED";
    public const string Rejected = "REJECTED";
}

public static class SyncCode
{
    public const string Invalid = "INVALID";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Unsupported = "UNSUPPORTED";
}

public sealed record SyncOperationDto(
    Guid Id,
    string OperationType,
    string EntityType,
    Guid EntityId,
    JsonElement Payload);

public sealed record SyncRequestDto(List<SyncOperationDto> Operations);

public sealed record SyncResultDto(
    Guid Id,
    string Status,
    string? Code,
    string? Message,
    ContactDto? Contact,
    LedgerEntryDto? Entry);

public sealed record SyncResponseDto(IReadOnlyList<SyncResultDto> Results);

public sealed class UnsupportedOperationException : Exception
{
    public UnsupportedOperationException(string message) : base(message)
    {
    }
}

public sealed class ApplySyncOperations
{
    public const int MaxOperations = 100;

    private const string ContactEntity = "CONTACT";
    private const string EntryEntity = "LEDGER_ENTRY";

    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    // What the phone puts in each operation's payload. Missing values arrive as defaults,
    // and the domain rules then refuse them with their usual messages.
    private sealed record ContactCreatePayload(
        Guid Id, string Name, string? Phone, string? Email, string? Notes, DateTimeOffset CreatedAt);

    private sealed record ContactUpdatePayload(
        Guid Id, string Name, string? Phone, string? Email, string? Notes, int BaseVersion);

    private sealed record EntryCreatePayload(
        Guid Id, Guid ContactId, TransactionType Type, long AmountCentavos, string? Description,
        DateTimeOffset OccurredAt, DateOnly? DueDate, DateTimeOffset CreatedAt, Guid? ReversesEntryId);

    private sealed record EntryReversePayload(
        Guid Id, Guid ReversesEntryId, long AmountCentavos, string? Description, DateTimeOffset OccurredAt);

    private readonly ISyncStore _store;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public ApplySyncOperations(ISyncStore store, ICurrentUser currentUser, TimeProvider clock)
    {
        _store = store;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<SyncResponseDto> ExecuteAsync(SyncRequestDto request, CancellationToken cancellationToken)
    {
        var operations = request.Operations;

        if (operations is null || operations.Count == 0)
            throw new DomainException("No operations were sent.");

        if (operations.Count > MaxOperations)
            throw new DomainException($"At most {MaxOperations} operations can be sent at once.");

        if (operations.Select(o => o.Id).Distinct().Count() != operations.Count)
            throw new DomainException("An operation id appears twice in one request.");

        var user = await _currentUser.GetAsync(cancellationToken);
        var now = _clock.GetUtcNow();
        var results = new List<SyncResultDto>();

        // Strictly in order: a later operation may depend on an earlier one.
        foreach (var operation in operations)
        {
            results.Add(await ProcessAsync(user.Id, operation, now, cancellationToken));
        }

        return new SyncResponseDto(results);
    }

    private async Task<SyncResultDto> ProcessAsync(
        Guid userId, SyncOperationDto operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Seen before? Then the answer is already decided: do not apply anything again.
        var existing = await _store.FindOperationAsync(userId, operation.Id, cancellationToken);

        if (existing is not null)
            return await ReplayAsync(userId, existing, cancellationToken);

        try
        {
            // The change and the record that it happened: one unit.
            return await _store.InTransactionAsync(async () =>
            {
                var result = await ApplyAsync(userId, operation, now, cancellationToken);

                await _store.RecordOperationAsync(
                    userId,
                    new StoredOperation(
                        operation.Id, operation.OperationType, operation.EntityType, operation.EntityId,
                        SyncStatus.Applied, null, null),
                    cancellationToken);

                return result;
            }, cancellationToken);
        }
        catch (DuplicateOperationException)
        {
            // A concurrent request handled this same operation first. Our attempt was rolled back.
            return await ReplayWinnerAsync(userId, operation, cancellationToken);
        }
        catch (Exception ex) when (IsRefusal(ex))
        {
            // A concurrent request may have handled this operation a moment ago. Its answer wins.
            var winner = await _store.FindOperationAsync(userId, operation.Id, cancellationToken);

            if (winner is not null)
                return await ReplayAsync(userId, winner, cancellationToken);

            var (code, message) = Describe(ex);

            // Remember the refusal, so a replay gets the same answer.
            try
            {
                await _store.InTransactionAsync(async () =>
                {
                    await _store.RecordOperationAsync(
                        userId,
                        new StoredOperation(
                            operation.Id, operation.OperationType, operation.EntityType, operation.EntityId,
                            SyncStatus.Rejected, code, message),
                        cancellationToken);

                    return 0;
                }, cancellationToken);
            }
            catch (DuplicateOperationException)
            {
                return await ReplayWinnerAsync(userId, operation, cancellationToken);
            }

            return Rejected(operation.Id, code, message);
        }
    }

    private async Task<SyncResultDto> ReplayWinnerAsync(
        Guid userId, SyncOperationDto operation, CancellationToken cancellationToken)
    {
        var winner = await _store.FindOperationAsync(userId, operation.Id, cancellationToken)
            ?? throw new InvalidOperationException("A duplicate operation id was reported, but none is stored.");

        return await ReplayAsync(userId, winner, cancellationToken);
    }

    // The answer to an operation that was already handled.
    private async Task<SyncResultDto> ReplayAsync(
        Guid userId, StoredOperation stored, CancellationToken cancellationToken)
    {
        if (stored.Status == SyncStatus.Rejected)
        {
            return Rejected(
                stored.Id, stored.Code ?? SyncCode.Invalid, stored.Message ?? "The change was refused.");
        }

        ContactDto? contact = null;
        LedgerEntryDto? entry = null;

        // The record's CURRENT state: it may have changed since, and that is what the phone should hold.
        if (stored.EntityType == ContactEntity)
        {
            var found = await _store.FindContactAsync(userId, stored.EntityId, cancellationToken);
            contact = found is null ? null : ContactDto.From(found);
        }
        else
        {
            var found = await _store.FindEntryAsync(userId, stored.EntityId, cancellationToken);
            entry = found is null ? null : LedgerEntryDto.From(found);
        }

        return new SyncResultDto(stored.Id, SyncStatus.AlreadyApplied, null, null, contact, entry);
    }

    private Task<SyncResultDto> ApplyAsync(
        Guid userId, SyncOperationDto operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (operation.Id == Guid.Empty || operation.EntityId == Guid.Empty)
            throw new DomainException("The operation has no id.");

        return (operation.EntityType, operation.OperationType) switch
        {
            (ContactEntity, "CREATE") => CreateContactAsync(userId, operation, cancellationToken),
            (ContactEntity, "UPDATE") => UpdateContactAsync(userId, operation, now, cancellationToken),
            (ContactEntity, "DELETE") => DeleteContactAsync(userId, operation, cancellationToken),
            (EntryEntity, "CREATE") => CreateEntryAsync(userId, operation, now, cancellationToken),
            (EntryEntity, "REVERSE") => ReverseEntryAsync(userId, operation, now, cancellationToken),
            _ => throw new UnsupportedOperationException(
                $"{operation.OperationType} on {operation.EntityType} is not supported.")
        };
    }

    // ---------- Contacts ----------

    private async Task<SyncResultDto> CreateContactAsync(
        Guid userId, SyncOperationDto operation, CancellationToken cancellationToken)
    {
        var payload = Parse<ContactCreatePayload>(operation.Payload);
        EnsureSameRecord(payload.Id, operation);

        if (await _store.ContactIdTakenAsync(payload.Id, cancellationToken))
            throw new ConflictException("That contact id is already in use.");

        var contact = Contact.Create(
            payload.Id, userId, payload.Name, payload.CreatedAt, payload.Phone, payload.Email, payload.Notes);

        await _store.AddContactAsync(contact, cancellationToken);

        return Applied(operation.Id, contact: ContactDto.From(contact));
    }

    private async Task<SyncResultDto> UpdateContactAsync(
        Guid userId, SyncOperationDto operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var payload = Parse<ContactUpdatePayload>(operation.Payload);
        EnsureSameRecord(payload.Id, operation);

        var contact = await _store.FindContactAsync(userId, payload.Id, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        // The edit was made on top of an older version of this contact. Applying it would
        // silently overwrite someone else's change, so nothing is applied.
        if (contact.Version != payload.BaseVersion)
            throw new ConflictException("This contact was changed somewhere else since your edit.");

        contact.Update(payload.Name, payload.Phone, payload.Email, payload.Notes, now);
        await _store.SaveContactChangesAsync(contact, cancellationToken);

        return Applied(operation.Id, contact: ContactDto.From(contact));
    }

    private async Task<SyncResultDto> DeleteContactAsync(
        Guid userId, SyncOperationDto operation, CancellationToken cancellationToken)
    {
        var contact = await _store.FindContactAsync(userId, operation.EntityId, cancellationToken);

        // Already gone (or never yours): the goal of the delete is reached.
        if (contact is null)
            return Applied(operation.Id);

        if (await _store.ContactHasEntriesAsync(contact.Id, cancellationToken))
            throw new ConflictException("This contact has transactions and cannot be deleted.");

        await _store.DeleteContactAsync(contact, cancellationToken);

        return Applied(operation.Id);
    }

    // ---------- Ledger entries ----------

    private async Task<SyncResultDto> CreateEntryAsync(
        Guid userId, SyncOperationDto operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var payload = Parse<EntryCreatePayload>(operation.Payload);
        EnsureSameRecord(payload.Id, operation);

        if (payload.ReversesEntryId is not null)
            throw new DomainException("Use a reverse operation to reverse an entry.");

        var contact = await _store.FindContactAsync(userId, payload.ContactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        LedgerEntry.EnsureNotTooFarInFuture(payload.OccurredAt, now);
        LedgerEntry.EnsureNotTooFarInFuture(payload.CreatedAt, now);

        if (await _store.EntryIdTakenAsync(payload.Id, cancellationToken))
            throw new ConflictException("That entry id is already in use.");

        var entry = LedgerEntry.Create(
            payload.Id,
            userId,
            contact.Id,
            payload.Type,
            Money.Php(payload.AmountCentavos / 100m),
            payload.OccurredAt,
            payload.CreatedAt,
            payload.Description,
            payload.DueDate);

        await _store.AddEntryAsync(entry, cancellationToken);

        return Applied(operation.Id, entry: LedgerEntryDto.From(entry));
    }

    private async Task<SyncResultDto> ReverseEntryAsync(
        Guid userId, SyncOperationDto operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var payload = Parse<EntryReversePayload>(operation.Payload);
        EnsureSameRecord(payload.Id, operation);

        var original = await _store.FindEntryAsync(userId, payload.ReversesEntryId, cancellationToken)
            ?? throw new NotFoundException("Ledger entry not found.");

        LedgerEntry.EnsureNotTooFarInFuture(payload.OccurredAt, now);

        // Built by the domain, which also refuses to reverse a reversal.
        var reversal = LedgerEntry.CreateReversal(payload.Id, original, payload.OccurredAt, payload.Description);

        // The phone computed this amount too. If they disagree, one side has a bug: refuse loudly.
        if ((long)(reversal.Amount.Amount * 100m) != payload.AmountCentavos)
            throw new DomainException("The reversal amount does not match the entry it reverses.");

        if (await _store.IsReversedAsync(original.Id, cancellationToken))
            throw new ConflictException("This entry has already been reversed.");

        if (await _store.EntryIdTakenAsync(payload.Id, cancellationToken))
            throw new ConflictException("That entry id is already in use.");

        await _store.AddEntryAsync(reversal, cancellationToken);

        return Applied(operation.Id, entry: LedgerEntryDto.From(reversal));
    }

    // ---------- Helpers ----------

    private static T Parse<T>(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            throw new DomainException("The operation has no data.");

        try
        {
            return payload.Deserialize<T>(PayloadOptions)
                ?? throw new DomainException("The operation data is not valid.");
        }
        catch (JsonException)
        {
            throw new DomainException("The operation data is not valid.");
        }
    }

    private static void EnsureSameRecord(Guid payloadId, SyncOperationDto operation)
    {
        if (payloadId != operation.EntityId)
            throw new DomainException("The operation refers to two different records.");
    }

    private static SyncResultDto Applied(Guid id, ContactDto? contact = null, LedgerEntryDto? entry = null) =>
        new(id, SyncStatus.Applied, null, null, contact, entry);

    private static SyncResultDto Rejected(Guid id, string code, string message) =>
        new(id, SyncStatus.Rejected, code, message, null, null);

    private static bool IsRefusal(Exception ex) =>
        ex is DomainException or NotFoundException or ConflictException or UnsupportedOperationException;

    private static (string Code, string Message) Describe(Exception ex) => ex switch
    {
        NotFoundException => (SyncCode.NotFound, ex.Message),
        ConflictException => (SyncCode.Conflict, ex.Message),
        UnsupportedOperationException => (SyncCode.Unsupported, ex.Message),
        _ => (SyncCode.Invalid, ex.Message)
    };
}