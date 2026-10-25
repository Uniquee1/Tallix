using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class BalanceCalculatorRunningTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();
    private static readonly DateTimeOffset Day1 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static LedgerEntry Entry(TransactionType type, decimal amount, DateTimeOffset when) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, type, Money.Php(amount), when, when);

    private static decimal[] Balances(IEnumerable<LedgerEntry> entries) =>
        BalanceCalculator.WithRunningBalance(entries, Currency.PHP)
            .Select(r => r.Balance.Amount)
            .ToArray();

    [Fact]
    public void NoEntries_ReturnsAnEmptyList()
    {
        Assert.Empty(BalanceCalculator.WithRunningBalance(Array.Empty<LedgerEntry>(), Currency.PHP));
    }

    [Fact]
    public void ReturnsTheBalanceAfterEachEntry()
    {
        // The example from your architecture document: 5000, minus 1000, plus 2000.
        var entries = new[]
        {
            Entry(TransactionType.Lent, 5000m, Day1),
            Entry(TransactionType.PaymentReceived, 1000m, Day1.AddDays(1)),
            Entry(TransactionType.Lent, 2000m, Day1.AddDays(2))
        };

        Assert.Equal(new[] { 5000m, 4000m, 6000m }, Balances(entries));
    }

    [Fact]
    public void OrdersByWhenItHappened_NotByTheOrderGiven()
    {
        var first = Entry(TransactionType.Lent, 5000m, Day1);
        var second = Entry(TransactionType.PaymentReceived, 1000m, Day1.AddDays(1));
        var third = Entry(TransactionType.Lent, 2000m, Day1.AddDays(2));

        Assert.Equal(new[] { 5000m, 4000m, 6000m }, Balances(new[] { third, first, second }));
    }

    [Fact]
    public void TheLastRunningBalance_EqualsTheTotalBalance()
    {
        var entries = new[]
        {
            Entry(TransactionType.Lent, 5000m, Day1),
            Entry(TransactionType.Borrowed, 700m, Day1.AddDays(1)),
            Entry(TransactionType.PaymentReceived, 1200m, Day1.AddDays(2))
        };

        var running = BalanceCalculator.WithRunningBalance(entries, Currency.PHP);

        Assert.Equal(BalanceCalculator.Calculate(entries, Currency.PHP), running[^1].Balance);
    }

    [Fact]
    public void AReversalBringsTheRunningBalanceBackDown()
    {
        var lent = Entry(TransactionType.Lent, 5000m, Day1);
        var reversal = LedgerEntry.CreateReversal(Guid.NewGuid(), lent, Day1.AddDays(1));

        Assert.Equal(new[] { 5000m, 0m }, Balances(new[] { lent, reversal }));
    }
}