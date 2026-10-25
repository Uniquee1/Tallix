using Tallix.Application.Dashboard;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class GetDashboardTests
{
    private static readonly DateOnly Today = new(2026, 10, 25);
    private static readonly DateTimeOffset Noon = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryContactRepository _contacts = new();
    private readonly InMemoryLedgerRepository _ledger = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly FixedTimeProvider _clock = new(Noon);
    private readonly GetDashboard _dashboard;

    public GetDashboardTests()
    {
        _dashboard = new GetDashboard(_ledger, _contacts, _currentUser, _clock);
    }

    private Contact SeedContact(string name, Guid? ownerId = null)
    {
        var contact = Contact.Create(Guid.NewGuid(), ownerId ?? _currentUser.User.Id, name, Noon);
        _contacts.Seed(contact);
        return contact;
    }

    private LedgerEntry SeedEntry(
        Contact contact, TransactionType type, decimal amount, DateOnly? due = null, int daysAgo = 0)
    {
        var when = Noon.AddDays(-daysAgo);
        var entry = LedgerEntry.Create(
            Guid.NewGuid(), contact.UserId, contact.Id, type, Money.Php(amount),
            when, when, dueDate: due);
        _ledger.Seed(entry);
        return entry;
    }

    private Task<DashboardDto> Get(DateOnly? asOf = null) =>
        _dashboard.ExecuteAsync(asOf ?? Today, CancellationToken.None);

    [Fact]
    public async Task ReturnsTheTotalsAndTheActiveContactCount()
    {
        var juan = SeedContact("Juan");
        var maria = SeedContact("Maria");
        SeedContact("Nobody");
        SeedEntry(juan, TransactionType.Lent, 5000m);
        SeedEntry(juan, TransactionType.PaymentReceived, 2000m);
        SeedEntry(maria, TransactionType.Borrowed, 1000m);

        var dto = await Get();

        Assert.Equal(3000m, dto.TotalOwedToMe);
        Assert.Equal(1000m, dto.TotalIOwe);
        Assert.Equal(2000m, dto.NetBalance);
        Assert.Equal(2, dto.ActiveContactCount);
    }

    [Fact]
    public async Task OnlyIncludesTheCurrentUsersData()
    {
        var mine = SeedContact("Mine");
        var theirs = SeedContact("Theirs", ownerId: Guid.NewGuid());
        SeedEntry(mine, TransactionType.Lent, 100m);
        var theirEntry = SeedEntry(theirs, TransactionType.Lent, 99999m, due: new DateOnly(2026, 1, 1));

        var dto = await Get();

        Assert.Equal(100m, dto.TotalOwedToMe);
        Assert.Equal(1, dto.ActiveContactCount);
        Assert.Equal(0, dto.Overdue.Count);
        Assert.DoesNotContain(dto.RecentTransactions, t => t.EntryId == theirEntry.Id);
    }

    [Fact]
    public async Task WithoutAnAsOfDate_UsesTodaysDateInUtc()
    {
        // 00:30 on the 25th in the Philippines is still 16:30 on the 24th in UTC.
        _clock.Now = new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.FromHours(8));

        var defaulted = await _dashboard.ExecuteAsync(null, CancellationToken.None);
        var explicitDate = await _dashboard.ExecuteAsync(Today, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 10, 24), defaulted.AsOf);
        Assert.Equal(Today, explicitDate.AsOf);
    }

    [Fact]
    public async Task OverdueItems_AreCapped_ButTheCountAndTotalAreNot()
    {
        var juan = SeedContact("Juan");

        for (var day = 1; day <= 7; day++)
            SeedEntry(juan, TransactionType.Lent, 100m, due: new DateOnly(2026, 10, day));

        var dto = await Get();

        Assert.Equal(7, dto.Overdue.Count);
        Assert.Equal(700m, dto.Overdue.OwedToMeTotal);
        Assert.Equal(GetDashboard.MaxDueItems, dto.Overdue.Items.Count);
        Assert.Equal(new DateOnly(2026, 10, 1), dto.Overdue.Items[0].DueDate);
    }

    [Fact]
    public async Task RecentTransactions_AreNewestFirst_AndCapped()
    {
        var juan = SeedContact("Juan");

        for (var daysAgo = 0; daysAgo < 12; daysAgo++)
            SeedEntry(juan, TransactionType.Lent, 100m, daysAgo: daysAgo);

        var dto = await Get();

        Assert.Equal(GetDashboard.RecentTransactionCount, dto.RecentTransactions.Count);
        Assert.Equal(Noon, dto.RecentTransactions[0].OccurredAt);
        Assert.Equal(Noon.AddDays(-9), dto.RecentTransactions[^1].OccurredAt);
        Assert.All(dto.RecentTransactions, t => Assert.Equal("Juan", t.ContactName));
    }

    [Fact]
    public async Task DueItems_CarryTheContactName_TheDirection_AndTheOutstandingAmount()
    {
        var juan = SeedContact("Juan");
        SeedEntry(juan, TransactionType.Lent, 1000m, due: new DateOnly(2026, 10, 10));
        SeedEntry(juan, TransactionType.PaymentReceived, 400m);

        var dto = await Get();

        var item = Assert.Single(dto.Overdue.Items);
        Assert.Equal("Juan", item.ContactName);
        Assert.Equal(DebtDirection.OwedToMe, item.Direction);
        Assert.Equal(600m, item.Outstanding);
        Assert.Equal(new DateOnly(2026, 10, 10), item.DueDate);
        Assert.Equal(600m, dto.Overdue.OwedToMeTotal);
        Assert.Equal(0m, dto.Overdue.IOweTotal);
    }
}