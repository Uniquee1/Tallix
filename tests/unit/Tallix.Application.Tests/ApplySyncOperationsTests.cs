using System.Text.Json;
using Tallix.Application.Common;
using Tallix.Application.Sync;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class ApplySyncOperationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemorySyncStore _store = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly ApplySyncOperations _sync;

    public ApplySyncOperationsTests()
    {
        _sync = new ApplySyncOperations(_store, _currentUser, new FixedTimeProvider(Now));
    }

    // ---------- Building operations the way the phone does ----------

    private static JsonElement Json(object payload) =>
        JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static SyncOperationDto Op(
        string operation, string entity, Guid entityId, object payload, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), operation, entity, entityId, Json(payload));

    private static SyncOperationDto CreateContact(Guid contactId, string name = "Juan", Guid? id = null) =>
        Op("CREATE", "CONTACT", contactId,
            new { id = contactId, name, phone = (string?)null, email = (string?)null, notes = (string?)null, createdAt = Now },
            id);

    private static SyncOperationDto UpdateContact(Guid contactId, int baseVersion, string name = "Juan S.") =>
        Op("UPDATE", "CONTACT", contactId,
            new { id = contactId, name, phone = (string?)null, email = (string?)null, notes = (string?)null, baseVersion });

    private static SyncOperationDto CreateEntry(
        Guid entryId, Guid contactId, long amountCentavos = 100000, DateTimeOffset? occurredAt = null) =>
        Op("CREATE", "LEDGER_ENTRY", entryId,
            new
            {
                id = entryId, contactId, type = "LENT", amountCentavos, description = (string?)null,
                occurredAt = occurredAt ?? Now, dueDate = (string?)null, createdAt = occurredAt ?? Now,
                reversesEntryId = (Guid?)null
            });

    private static SyncOperationDto Reverse(Guid reversalId, Guid originalId, long amountCentavos = -100000) =>
        Op("REVERSE", "LEDGER_ENTRY", reversalId,
            new { id = reversalId, reversesEntryId = originalId, amountCentavos, description = (string?)null, occurredAt = Now });

    private async Task<IReadOnlyList<SyncResultDto>> Send(params SyncOperationDto[] operations) =>
        (await _sync.ExecuteAsync(new SyncRequestDto(operations.ToList()), CancellationToken.None)).Results;

    private async Task<SyncResultDto> SendOne(SyncOperationDto operation) => (await Send(operation))[0];

    private Contact SeedContact(Guid? ownerId = null)
    {
        var contact = Contact.Create(Guid.NewGuid(), ownerId ?? _currentUser.User.Id, "Seeded", Now);
        _store.Contacts[contact.Id] = contact;
        return contact;
    }

    // ---------- Contacts ----------

    [Fact]
    public async Task CreatingAContact_IsApplied_AtVersionOne()
    {
        var id = Guid.NewGuid();

        var result = await SendOne(CreateContact(id));

        Assert.Equal(SyncStatus.Applied, result.Status);
        Assert.Equal(1, result.Contact!.Version);
        Assert.Equal(_currentUser.User.Id, _store.Contacts[id].UserId);
    }

    [Fact]
    public async Task ReplayingAnOperation_AcknowledgesIt_WithoutApplyingItAgain()
    {
        var operation = CreateContact(Guid.NewGuid());

        await SendOne(operation);
        var replay = await SendOne(operation);

        Assert.Equal(SyncStatus.AlreadyApplied, replay.Status);
        Assert.NotNull(replay.Contact);
        Assert.Single(_store.Contacts);
    }

    [Fact]
    public async Task ReplayingARefusedOperation_ReturnsTheSameRefusal()
    {
        var operation = CreateEntry(Guid.NewGuid(), Guid.NewGuid()); // no such contact

        var first = await SendOne(operation);
        var replay = await SendOne(operation);

        Assert.Equal(SyncCode.NotFound, first.Code);
        Assert.Equal(SyncStatus.Rejected, replay.Status);
        Assert.Equal(first.Code, replay.Code);
        Assert.Equal(first.Message, replay.Message);
    }

    [Fact]
    public async Task AnEditBasedOnTheCurrentVersion_IsApplied_AndRaisesTheVersion()
    {
        var contact = SeedContact();

        var result = await SendOne(UpdateContact(contact.Id, baseVersion: 1));

        Assert.Equal(SyncStatus.Applied, result.Status);
        Assert.Equal(2, result.Contact!.Version);
        Assert.Equal("Juan S.", _store.Contacts[contact.Id].Name);
    }

    [Fact]
    public async Task AnEditBasedOnAnOldVersion_IsRefused_AndChangesNothing()
    {
        var contact = SeedContact();
        contact.Update("Changed elsewhere", null, null, null, Now); // version 2

        var result = await SendOne(UpdateContact(contact.Id, baseVersion: 1));

        Assert.Equal(SyncStatus.Rejected, result.Status);
        Assert.Equal(SyncCode.Conflict, result.Code);
        Assert.Equal("Changed elsewhere", _store.Contacts[contact.Id].Name);
    }

    [Fact]
    public async Task DeletingAContactThatIsGone_CountsAsDone()
    {
        var result = await SendOne(Op("DELETE", "CONTACT", Guid.NewGuid(), new { }));

        Assert.Equal(SyncStatus.Applied, result.Status);
    }

    [Fact]
    public async Task DeletingAContactWithTransactions_IsRefused()
    {
        var contactId = Guid.NewGuid();
        var results = await Send(CreateContact(contactId), CreateEntry(Guid.NewGuid(), contactId));
        Assert.All(results, r => Assert.Equal(SyncStatus.Applied, r.Status));

        var result = await SendOne(Op("DELETE", "CONTACT", contactId, new { }));

        Assert.Equal(SyncCode.Conflict, result.Code);
        Assert.True(_store.Contacts.ContainsKey(contactId));
    }

    [Fact]
    public async Task AnIdAlreadyUsedByAnotherUser_IsAConflict_AndNothingIsOverwritten()
    {
        var theirs = SeedContact(ownerId: Guid.NewGuid());

        var result = await SendOne(CreateContact(theirs.Id, name: "Mine now?"));

        Assert.Equal(SyncCode.Conflict, result.Code);
        Assert.Equal("Seeded", _store.Contacts[theirs.Id].Name);
    }

    // ---------- Entries ----------

    [Fact]
    public async Task RecordingAnEntry_IsApplied()
    {
        var contact = SeedContact();

        var result = await SendOne(CreateEntry(Guid.NewGuid(), contact.Id, amountCentavos: 250000));

        Assert.Equal(SyncStatus.Applied, result.Status);
        Assert.Equal(2500m, result.Entry!.Amount);
    }

    [Fact]
    public async Task AnEntryForAnotherUsersContact_IsNotFound()
    {
        var theirs = SeedContact(ownerId: Guid.NewGuid());

        var result = await SendOne(CreateEntry(Guid.NewGuid(), theirs.Id));

        Assert.Equal(SyncCode.NotFound, result.Code);
        Assert.Empty(_store.Entries);
    }

    [Fact]
    public async Task AnEntryDatedMoreThanADayAhead_IsRefused_WithAClockMessage()
    {
        var contact = SeedContact();

        var result = await SendOne(CreateEntry(Guid.NewGuid(), contact.Id, occurredAt: Now.AddDays(2)));

        Assert.Equal(SyncCode.Invalid, result.Code);
        Assert.Contains("clock", result.Message);
        Assert.Empty(_store.Entries);
    }

    [Fact]
    public async Task AnEntryDatedInThePast_IsAccepted()
    {
        var contact = SeedContact();

        var result = await SendOne(CreateEntry(Guid.NewGuid(), contact.Id, occurredAt: Now.AddDays(-400)));

        Assert.Equal(SyncStatus.Applied, result.Status);
    }

    [Fact]
    public async Task AnEntryThatBreaksADomainRule_IsRefusedWithThatMessage()
    {
        var contact = SeedContact();

        var result = await SendOne(CreateEntry(Guid.NewGuid(), contact.Id, amountCentavos: 0));

        Assert.Equal(SyncCode.Invalid, result.Code);
        Assert.Equal("Amount must be greater than zero.", result.Message);
    }

    // ---------- Reversals ----------

    [Fact]
    public async Task AReversal_IsApplied_WithTheOppositeAmount()
    {
        var contactId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var reversalId = Guid.NewGuid();

        var results = await Send(
            CreateContact(contactId), CreateEntry(entryId, contactId), Reverse(reversalId, entryId));

        Assert.All(results, r => Assert.Equal(SyncStatus.Applied, r.Status));
        Assert.Equal(entryId, _store.Entries[reversalId].ReversesEntryId);
        Assert.Equal(0m, BalanceCalculator.Calculate(_store.Entries.Values, Currency.PHP).Amount);
    }

    [Fact]
    public async Task AReversalWithTheWrongAmount_IsRefused()
    {
        var contactId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        await Send(CreateContact(contactId), CreateEntry(entryId, contactId));

        var result = await SendOne(Reverse(Guid.NewGuid(), entryId, amountCentavos: -5));

        Assert.Equal(SyncCode.Invalid, result.Code);
        Assert.Single(_store.Entries);
    }

    [Fact]
    public async Task ReversingTheSameEntryTwice_IsRefused()
    {
        var contactId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        await Send(CreateContact(contactId), CreateEntry(entryId, contactId), Reverse(Guid.NewGuid(), entryId));

        var second = await SendOne(Reverse(Guid.NewGuid(), entryId));

        Assert.Equal(SyncCode.Conflict, second.Code);
        Assert.Equal("This entry has already been reversed.", second.Message);
    }

    [Fact]
    public async Task ReversingAReversal_IsRefused()
    {
        var contactId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var reversalId = Guid.NewGuid();
        await Send(CreateContact(contactId), CreateEntry(entryId, contactId), Reverse(reversalId, entryId));

        var result = await SendOne(Reverse(Guid.NewGuid(), reversalId, amountCentavos: 100000));

        Assert.Equal(SyncCode.Invalid, result.Code);
    }

    [Fact]
    public async Task ReversingAnEntryThatDoesNotExist_IsNotFound()
    {
        var result = await SendOne(Reverse(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(SyncCode.NotFound, result.Code);
    }

    // ---------- Batches ----------

    [Fact]
    public async Task ARefusedOperation_DoesNotStopTheOnesAfterIt()
    {
        var contactId = Guid.NewGuid();

        var results = await Send(
            CreateEntry(Guid.NewGuid(), Guid.NewGuid()), // refused: no such contact
            CreateContact(contactId));                   // still applied

        Assert.Equal(SyncStatus.Rejected, results[0].Status);
        Assert.Equal(SyncStatus.Applied, results[1].Status);
    }

    [Fact]
    public async Task UnknownOperations_AreRefusedAsUnsupported()
    {
        var result = await SendOne(Op("SHRED", "CONTACT", Guid.NewGuid(), new { }));

        Assert.Equal(SyncCode.Unsupported, result.Code);
    }

    [Fact]
    public async Task AGarbledPayload_IsRefusedAsInvalid()
    {
        var operation = new SyncOperationDto(
            Guid.NewGuid(), "CREATE", "CONTACT", Guid.NewGuid(), JsonSerializer.SerializeToElement("not an object"));

        var result = await SendOne(operation);

        Assert.Equal(SyncCode.Invalid, result.Code);
    }

    [Fact]
    public async Task ALostRaceOnTheOperationId_IsRolledBack_AndAnswersWithTheWinner()
    {
        var contactId = Guid.NewGuid();
        var operation = CreateContact(contactId);

        _store.ConcurrentWinner = new StoredOperation(
            operation.Id, "CREATE", "CONTACT", contactId, SyncStatus.Applied, null, null);

        var result = await SendOne(operation);

        Assert.Equal(SyncStatus.AlreadyApplied, result.Status);
        Assert.Empty(_store.Contacts); // our attempt's change was rolled back
    }

    [Fact]
    public async Task ARefusalThatRacesAWinner_AnswersWithTheWinnersResult()
    {
        var theirs = SeedContact(ownerId: Guid.NewGuid());
        var operation = CreateContact(theirs.Id); // would be refused: the id is taken

        _store.LateWinner = new StoredOperation(
            operation.Id, "CREATE", "CONTACT", theirs.Id, SyncStatus.Applied, null, null);

        var result = await SendOne(operation);

        Assert.Equal(SyncStatus.AlreadyApplied, result.Status);
    }

    [Fact]
    public async Task ABatchWithNoOperations_IsRejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => Send());
    }

    [Fact]
    public async Task ABatchOverTheLimit_IsRejected()
    {
        var operations = Enumerable.Range(0, ApplySyncOperations.MaxOperations + 1)
            .Select(_ => CreateContact(Guid.NewGuid()))
            .ToArray();

        await Assert.ThrowsAsync<DomainException>(() => Send(operations));
        Assert.Empty(_store.Contacts);
    }

    [Fact]
    public async Task ABatchWithTheSameOperationIdTwice_IsRejected()
    {
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainException>(() =>
            Send(CreateContact(Guid.NewGuid(), id: id), CreateContact(Guid.NewGuid(), id: id)));
    }
}