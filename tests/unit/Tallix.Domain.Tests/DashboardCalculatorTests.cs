using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class DashboardCalculatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 25);
    private static readonly DateTimeOffset Start = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private static LedgerEntry Entry(
        Guid contactId, TransactionType type, decimal amount, int day = 0, DateOnly? due = null) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, contactId, type, Money.Php(amount),
            Start.AddDays(day), Start.AddDays(day), dueDate: due);

    private static DashboardSummary Summary(params LedgerEntry[] entries) =>
        DashboardCalculator.Calculate(entries, Today, Currency.PHP);

    [Fact]
    public void NoEntries_IsAllZero()
    {
        var summary = Summary();

        Assert.Equal(0m, summary.TotalOwedToMe.Amount);
        Assert.Equal(0m, summary.TotalIOwe.Amount);
        Assert.Equal(0m, summary.NetBalance.Amount);
        Assert.Equal(0, summary.ActiveContactCount);
        Assert.Empty(summary.Overdue);
        Assert.Empty(summary.Upcoming);
    }

    [Fact]
    public void Balances_AreNettedWithinAContact_ButNotAcrossContacts()
    {
        var juan = Guid.NewGuid();
        var maria = Guid.NewGuid();

        var summary = Summary(
            Entry(juan, TransactionType.Lent, 5000m),
            Entry(juan, TransactionType.Borrowed, 1000m),    // BR-006: Juan nets to 4,000 owed to me
            Entry(maria, TransactionType.Borrowed, 1000m));  // Maria stays separate: 1,000 I owe

        Assert.Equal(4000m, summary.TotalOwedToMe.Amount);
        Assert.Equal(1000m, summary.TotalIOwe.Amount);
        Assert.Equal(3000m, summary.NetBalance.Amount);
        Assert.Equal(2, summary.ActiveContactCount);
    }

    [Fact]
    public void ASettledContact_IsNotActive()
    {
        var juan = Guid.NewGuid();

        var summary = Summary(
            Entry(juan, TransactionType.Lent, 100m),
            Entry(juan, TransactionType.PaymentReceived, 100m, day: 1));

        Assert.Equal(0, summary.ActiveContactCount);
        Assert.Equal(0m, summary.TotalOwedToMe.Amount);
    }

    [Fact]
    public void DebtsAreSortedIntoOverdue_Upcoming_AndLater()
    {
        var juan = Guid.NewGuid();

        var summary = Summary(
            Entry(juan, TransactionType.Lent, 100m, due: new DateOnly(2026, 10, 24)),  // yesterday
            Entry(juan, TransactionType.Lent, 200m, due: new DateOnly(2026, 10, 25)),  // today
            Entry(juan, TransactionType.Lent, 300m, due: new DateOnly(2026, 11, 1)),   // exactly 7 days
            Entry(juan, TransactionType.Lent, 400m, due: new DateOnly(2026, 11, 2)));  // 8 days: neither

        var overdue = Assert.Single(summary.Overdue);
        Assert.Equal(100m, overdue.Outstanding.Amount);

        Assert.Equal(
            new[] { new DateOnly(2026, 10, 25), new DateOnly(2026, 11, 1) },
            summary.Upcoming.Select(d => d.Entry.DueDate!.Value));
        Assert.Equal(new[] { 200m, 300m }, summary.Upcoming.Select(d => d.Outstanding.Amount));
    }

    [Fact]
    public void APaymentOnTheOldestDebt_RemovesItFromOverdue()
    {
        var juan = Guid.NewGuid();

        var summary = Summary(
            Entry(juan, TransactionType.Lent, 1000m, due: new DateOnly(2026, 10, 10)),
            Entry(juan, TransactionType.Lent, 1000m, day: 1, due: new DateOnly(2026, 11, 10)),
            Entry(juan, TransactionType.PaymentReceived, 1000m, day: 2));

        Assert.Empty(summary.Overdue);
        Assert.Empty(summary.Upcoming);
    }

    [Fact]
    public void APartialPayment_LeavesTheShortfallOverdue()
    {
        var juan = Guid.NewGuid();

        var summary = Summary(
            Entry(juan, TransactionType.Lent, 1000m, due: new DateOnly(2026, 10, 10)),
            Entry(juan, TransactionType.Lent, 1000m, day: 1, due: new DateOnly(2026, 11, 10)),
            Entry(juan, TransactionType.PaymentReceived, 500m, day: 2));

        var overdue = Assert.Single(summary.Overdue);
        Assert.Equal(500m, overdue.Outstanding.Amount);
    }

    [Fact]
    public void UndatedDebts_AreNeverOverdueOrUpcoming()
    {
        var summary = Summary(Entry(Guid.NewGuid(), TransactionType.Lent, 100m));

        Assert.Empty(summary.Overdue);
        Assert.Empty(summary.Upcoming);
        Assert.Equal(1, summary.ActiveContactCount);
        Assert.Equal(100m, summary.TotalOwedToMe.Amount);
    }

    [Fact]
    public void Overdue_IsOrderedByDueDate_MostOverdueFirst()
    {
        var summary = Summary(
            Entry(Guid.NewGuid(), TransactionType.Lent, 100m, due: new DateOnly(2026, 10, 20)),
            Entry(Guid.NewGuid(), TransactionType.Lent, 100m, due: new DateOnly(2026, 10, 5)));

        Assert.Equal(
            new[] { new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 20) },
            summary.Overdue.Select(d => d.Entry.DueDate!.Value));
    }

    [Fact]
    public void AsOfAtTheEndOfTheCalendar_DoesNotOverflow()
    {
        var entries = new[]
        {
            Entry(Guid.NewGuid(), TransactionType.Lent, 100m, due: new DateOnly(2026, 10, 1))
        };

        var summary = DashboardCalculator.Calculate(entries, DateOnly.MaxValue, Currency.PHP);

        Assert.Single(summary.Overdue);
    }
}