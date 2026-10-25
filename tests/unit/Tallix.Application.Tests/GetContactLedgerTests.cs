using Tallix.Application.Common;
using Tallix.Application.Ledger;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class GetContactLedgerTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryContactRepository _contacts = new();
    private readonly InMemoryLedgerRepository _ledger = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly GetContactLedger _getLedger;

    public GetContactLedgerTests()
    {
        _getLedger = new GetContactLedger(_ledger, _contacts, _currentUser);
    }

    private Contact SeedContact(Guid? ownerId = null)
    {
        var contact = Contact.Create(
            Guid.NewGuid(), ownerId ?? _currentUser.User.Id, "Juan", Day1);
        _contacts.Seed(contact);
        return contact;
    }

    private LedgerEntry SeedEntry(
        Guid userId, Guid contactId, TransactionType type, decimal amount, int day)
    {
        var when = Day1.AddDays(day);
        var entry = LedgerEntry.Create(
            Guid.NewGuid(), userId, contactId, type, Money.Php(amount), when, when);
        _ledger.Seed(entry);
        return entry;
    }

    private Task<ContactLedgerDto> Get(Guid contactId, int page = 1, int pageSize = 20) =>
        _getLedger.ExecuteAsync(contactId, page, pageSize, CancellationToken.None);

    [Fact]
    public async Task ReturnsNewestFirst_WithTheRunningBalanceOnEachRow()
    {
        var contact = SeedContact();
        var uid = _currentUser.User.Id;
        SeedEntry(uid, contact.Id, TransactionType.Lent, 5000m, 0);
        SeedEntry(uid, contact.Id, TransactionType.PaymentReceived, 1000m, 1);
        SeedEntry(uid, contact.Id, TransactionType.Lent, 2000m, 2);

        var result = await Get(contact.Id);

        Assert.Equal(6000m, result.Balance);
        Assert.Equal(new[] { 6000m, 4000m, 5000m }, result.History.Items.Select(i => i.RunningBalance));
        Assert.Equal(new[] { 2000m, -1000m, 5000m }, result.History.Items.Select(i => i.SignedAmount));
    }

    [Fact]
    public async Task MarksWhichEntriesWereReversed_AndWhichAreReversals()
    {
        var contact = SeedContact();
        var original = SeedEntry(_currentUser.User.Id, contact.Id, TransactionType.Lent, 100m, 0);
        var reversal = LedgerEntry.CreateReversal(Guid.NewGuid(), original, Day1.AddDays(1));
        _ledger.Seed(reversal);

        var result = await Get(contact.Id);

        Assert.Equal(0m, result.Balance);

        var originalRow = result.History.Items.Single(i => i.Id == original.Id);
        var reversalRow = result.History.Items.Single(i => i.Id == reversal.Id);

        Assert.Equal(reversal.Id, originalRow.ReversedByEntryId);
        Assert.Null(originalRow.ReversesEntryId);
        Assert.Equal(original.Id, reversalRow.ReversesEntryId);
        Assert.Null(reversalRow.ReversedByEntryId);
    }

    [Fact]
    public async Task OnlyIncludesThisContactsEntries()
    {
        var juan = SeedContact();
        var maria = SeedContact();
        var uid = _currentUser.User.Id;
        SeedEntry(uid, juan.Id, TransactionType.Lent, 100m, 0);
        SeedEntry(uid, maria.Id, TransactionType.Lent, 999m, 1);
        SeedEntry(Guid.NewGuid(), Guid.NewGuid(), TransactionType.Lent, 777m, 2);

        var result = await Get(juan.Id);

        var item = Assert.Single(result.History.Items);
        Assert.Equal(100m, item.Amount);
        Assert.Equal(100m, result.Balance);
    }

    [Fact]
    public async Task LaterPages_StillShowTheCorrectRunningBalance()
    {
        var contact = SeedContact();

        for (var day = 0; day < 5; day++)
            SeedEntry(_currentUser.User.Id, contact.Id, TransactionType.Lent, 100m, day);

        var result = await Get(contact.Id, page: 2, pageSize: 2);

        // Newest first: [500, 400] [300, 200] [100]. Page 2 is the middle pair.
        Assert.Equal(new[] { 300m, 200m }, result.History.Items.Select(i => i.RunningBalance));
        Assert.Equal(5, result.History.TotalCount);
        Assert.Equal(500m, result.Balance);
    }

    [Fact]
    public async Task ContactWithNoEntries_HasAZeroBalance()
    {
        var contact = SeedContact();

        var result = await Get(contact.Id);

        Assert.Equal(0m, result.Balance);
        Assert.Empty(result.History.Items);
        Assert.Equal(0, result.History.TotalCount);
    }

    [Fact]
    public async Task AnotherUsersContact_IsNotFound()
    {
        var theirs = SeedContact(ownerId: Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => Get(theirs.Id));
    }
}