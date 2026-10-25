using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Application.Users;
using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Ledger;

public sealed record RecordLedgerEntryRequest(
    TransactionType? Type,
    decimal? Amount,
    Currency? Currency,
    DateTimeOffset? OccurredAt,
    DateOnly? DueDate,
    string? Description);

public sealed class RecordLedgerEntry
{
    private readonly ILedgerRepository _ledger;
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public RecordLedgerEntry(
        ILedgerRepository ledger,
        IContactRepository contacts,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _ledger = ledger;
        _contacts = contacts;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<LedgerEntryDto> ExecuteAsync(
        Guid contactId,
        RecordLedgerEntryRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        // BR-003: the contact must belong to the current user. Loading it through the
        // owner-scoped repository means someone else's contact looks like a missing one.
        var contact = await _contacts.GetByIdAsync(user.Id, contactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        if (request.Type is null)
            throw new DomainException("Transaction type is required.");

        if (request.Amount is null)
            throw new DomainException("Amount is required.");

        var now = _clock.GetUtcNow();

        var entry = LedgerEntry.Create(
            Guid.NewGuid(),
            user.Id,
            contact.Id,
            request.Type.Value,
            new Money(request.Amount.Value, request.Currency ?? Currency.PHP),
            request.OccurredAt ?? now,
            now,
            request.Description,
            request.DueDate);

        if (!await _ledger.TryAddAsync(entry, cancellationToken))
            throw new NotFoundException("Contact not found.");

        return LedgerEntryDto.From(entry);
    }
}