using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class OutstandingDebtCalculatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Oct10 = new(2026, 10, 10);
    private static readonly DateOnly Nov10 = new(2026, 11, 10);

    private static LedgerEntry Entry(TransactionType type, decimal amount, int day, DateOnly? due = null) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, type, Money.Php(amount),
            Start.AddDays(day), Start.AddDays(day), dueDate: due);

    private static IReadOnlyList<OutstandingDebt> Debts(params LedgerEntry[] entries) =>
        OutstandingDebtCalculator.Calculate(entries, Currency.PHP);

    [Fact]
    public void NoEntries_HasNothingOutstanding()
    {
        Assert.Empty(Debts());
    }

    [Fact]
    public void FullySettled_HasNothingOutstanding()
    {
        var debts = Debts(
            Entry(TransactionType.Lent, 1000m, 0, Oct10),
            Entry(TransactionType.PaymentReceived, 1000m, 1));

        Assert.Empty(debts);
    }

    [Fact]
    public void APayment_SettlesTheOldestDueDateFirst()
    {
        var older = Entry(TransactionType.Lent, 1000m, 0, Oct10);
        var newer = Entry(TransactionType.Lent, 1000m, 1, Nov10);

        // Given newest first, to prove the order comes from the due dates.
        var debts = Debts(newer, older, Entry(TransactionType.PaymentReceived, 1000m, 2));

        var debt = Assert.Single(debts);
        Assert.Equal(newer.Id, debt.Entry.Id);
        Assert.Equal(1000m, debt.Outstanding.Amount);
        Assert.Equal(DebtDirection.OwedToMe, debt.Direction);
    }

    [Fact]
    public void APartialPayment_LeavesTheShortfallOnTheOldestDebt()
    {
        var older = Entry(TransactionType.Lent, 1000m, 0, Oct10);
        var newer = Entry(TransactionType.Lent, 1000m, 1, Nov10);

        var debts = Debts(older, newer, Entry(TransactionType.PaymentReceived, 500m, 2));

        Assert.Equal(2, debts.Count);
        Assert.Equal(older.Id, debts[0].Entry.Id);
        Assert.Equal(500m, debts[0].Outstanding.Amount);
        Assert.Equal(newer.Id, debts[1].Entry.Id);
        Assert.Equal(1000m, debts[1].Outstanding.Amount);
    }

    [Fact]
    public void AReversedEntry_IsNotCountedAsADebt()
    {
        var reversed = Entry(TransactionType.Lent, 1000m, 0, new DateOnly(2026, 12, 1));
        var reversal = LedgerEntry.CreateReversal(Guid.NewGuid(), reversed, Start.AddDays(1));
        var kept = Entry(TransactionType.Lent, 500m, 2, Nov10);

        var debts = Debts(reversed, reversal, kept);

        var debt = Assert.Single(debts);
        Assert.Equal(kept.Id, debt.Entry.Id);
        Assert.Equal(500m, debt.Outstanding.Amount);
    }

    [Fact]
    public void Payments_SettleDatedDebtsBeforeUndatedOnes()
    {
        var undated = Entry(TransactionType.Lent, 1000m, 0);
        var dated = Entry(TransactionType.Lent, 1000m, 1, Oct10);

        var debts = Debts(undated, dated, Entry(TransactionType.PaymentReceived, 1000m, 2));

        var debt = Assert.Single(debts);
        Assert.Equal(undated.Id, debt.Entry.Id);
        Assert.Equal(1000m, debt.Outstanding.Amount);
    }

    [Fact]
    public void WhenIOwe_TheDirectionIsIOwe_AndPaymentsMadeReduceIt()
    {
        var borrowed = Entry(TransactionType.Borrowed, 800m, 0, Oct10);

        var debts = Debts(borrowed, Entry(TransactionType.PaymentMade, 300m, 1));

        var debt = Assert.Single(debts);
        Assert.Equal(DebtDirection.IOwe, debt.Direction);
        Assert.Equal(500m, debt.Outstanding.Amount);
    }

    [Fact]
    public void AnOppositeEntry_ReducesWhatIsOutstanding()
    {
        var lent = Entry(TransactionType.Lent, 1000m, 0, Oct10);

        var debts = Debts(lent, Entry(TransactionType.Borrowed, 300m, 1));

        var debt = Assert.Single(debts);
        Assert.Equal(lent.Id, debt.Entry.Id);
        Assert.Equal(700m, debt.Outstanding.Amount);
    }

    [Fact]
    public void ABalanceWithNoMatchingDebt_HasNothingOutstanding()
    {
        Assert.Empty(Debts(Entry(TransactionType.Adjustment, 500m, 0)));
    }

    [Fact]
    public void EntriesOfSeveralContacts_AreRejected()
    {
        var otherContact = LedgerEntry.Create(
            Guid.NewGuid(), UserId, Guid.NewGuid(), TransactionType.Lent,
            Money.Php(1m), Start, Start);

        Assert.Throws<DomainException>(() =>
            Debts(Entry(TransactionType.Lent, 100m, 0), otherContact));
    }
}