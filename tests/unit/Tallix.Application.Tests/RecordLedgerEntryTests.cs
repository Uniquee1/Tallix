using Tallix.Application.Common;
using Tallix.Application.Ledger;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class RecordLedgerEntryTests
{
    private readonly InMemoryContactRepository _contacts = new();
    private readonly InMemoryLedgerRepository _ledger = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly FixedTimeProvider _clock =
        new(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly RecordLedgerEntry _record;

    public RecordLedgerEntryTests()
    {
        _record = new RecordLedgerEntry(_ledger, _contacts, _currentUser, _clock);
    }

    private Contact SeedContact(Guid? ownerId = null)
    {
        var contact = Contact.Create(
            Guid.NewGuid(), ownerId ?? _currentUser.User.Id, "Juan", _clock.GetUtcNow());
        _contacts.Seed(contact);
        return contact;
    }

    private Task<LedgerEntryDto> Record(
        Guid contactId, TransactionType? type, decimal? amount, DateTimeOffset? occurredAt = null) =>
        _record.ExecuteAsync(
            contactId,
            new RecordLedgerEntryRequest(type, amount, null, occurredAt, null, null),
            CancellationToken.None);

    [Fact]
    public async Task Record_StoresTheEntryForTheCurrentUserAndContact()
    {
        var contact = SeedContact();

        var dto = await Record(contact.Id, TransactionType.Lent, 5000m);

        Assert.Equal(5000m, dto.Amount);
        Assert.Equal(5000m, dto.SignedAmount);
        Assert.Equal(Currency.PHP, dto.Currency);

        var stored = Assert.Single(_ledger.Entries);
        Assert.Equal(_currentUser.User.Id, stored.UserId);
        Assert.Equal(contact.Id, stored.ContactId);
    }

    [Fact]
    public async Task Record_Borrowed_HasANegativeSignedAmount()
    {
        var contact = SeedContact();

        var dto = await Record(contact.Id, TransactionType.Borrowed, 1000m);

        Assert.Equal(1000m, dto.Amount);
        Assert.Equal(-1000m, dto.SignedAmount);
    }

    [Fact]
    public async Task Record_WithoutOccurredAt_UsesTheServerTime()
    {
        var contact = SeedContact();

        var dto = await Record(contact.Id, TransactionType.Lent, 100m);

        Assert.Equal(_clock.Now, dto.OccurredAt);
        Assert.Equal(_clock.Now, dto.CreatedAt);
    }

    [Fact]
    public async Task Record_OnAnotherUsersContact_IsNotFound_AndStoresNothing()
    {
        var theirs = SeedContact(ownerId: Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(
            () => Record(theirs.Id, TransactionType.Lent, 100m));

        Assert.Empty(_ledger.Entries);
    }

    [Fact]
    public async Task Record_OnAnUnknownContact_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Record(Guid.NewGuid(), TransactionType.Lent, 100m));
    }

    [Fact]
    public async Task Record_WithoutAType_IsRejected()
    {
        var contact = SeedContact();

        await Assert.ThrowsAsync<DomainException>(() => Record(contact.Id, null, 100m));

        Assert.Empty(_ledger.Entries);
    }

    [Fact]
    public async Task Record_WithoutAnAmount_IsRejected()
    {
        var contact = SeedContact();

        await Assert.ThrowsAsync<DomainException>(
            () => Record(contact.Id, TransactionType.Lent, null));

        Assert.Empty(_ledger.Entries);
    }

    [Fact]
    public async Task Record_ZeroAmount_IsRejected()
    {
        var contact = SeedContact();

        await Assert.ThrowsAsync<DomainException>(
            () => Record(contact.Id, TransactionType.Lent, 0m));

        Assert.Empty(_ledger.Entries);
    }

    [Fact]
    public async Task Record_MoreThanTwoDecimalPlaces_IsRejected()
    {
        var contact = SeedContact();

        await Assert.ThrowsAsync<DomainException>(
            () => Record(contact.Id, TransactionType.Lent, 10.123m));

        Assert.Empty(_ledger.Entries);
    }

    [Fact]
    public async Task Record_WhenTheContactVanishesBeforeTheInsert_IsNotFound()
    {
        var contact = SeedContact();
        _ledger.RejectNextAdd = true;

        await Assert.ThrowsAsync<NotFoundException>(
            () => Record(contact.Id, TransactionType.Lent, 100m));

        Assert.Empty(_ledger.Entries);
    }
}