using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class BalanceCalculatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();

    private static LedgerEntry Entry(TransactionType type, decimal amount) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, type, Money.Php(amount),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static decimal Balance(params LedgerEntry[] entries) =>
        BalanceCalculator.Calculate(entries, Currency.PHP).Amount;

    [Fact]
    public void NoEntries_BalanceIsZero()
    {
        Assert.Equal(0m, Balance());
    }

    [Fact]
    public void LentPaymentLent_Is6000()
    {
        // The example from your architecture document.
        var balance = Balance(
            Entry(TransactionType.Lent, 5000m),
            Entry(TransactionType.PaymentReceived, 1000m),
            Entry(TransactionType.Lent, 2000m));

        Assert.Equal(6000m, balance);
    }

    [Fact]
    public void PartialPayment_LeavesRemainder()
    {
        // FR-007: original 5,000, payment 2,000, remaining 3,000.
        var balance = Balance(
            Entry(TransactionType.Lent, 5000m),
            Entry(TransactionType.PaymentReceived, 2000m));

        Assert.Equal(3000m, balance);
    }

    [Fact]
    public void BorrowingFromSomeoneWhoOwesYou_NetsOut()
    {
        // BR-006: they owe me 5,000, I borrow 1,000, net 4,000 owed to me.
        var balance = Balance(
            Entry(TransactionType.Lent, 5000m),
            Entry(TransactionType.Borrowed, 1000m));

        Assert.Equal(4000m, balance);
    }

    [Fact]
    public void OnlyBorrowed_IsNegative()
    {
        var balance = Balance(Entry(TransactionType.Borrowed, 800m));

        Assert.Equal(-800m, balance);
    }

    [Fact]
    public void FullyRepaid_IsZero()
    {
        // BR-007: zero balance means nothing outstanding.
        var balance = Balance(
            Entry(TransactionType.Lent, 5000m),
            Entry(TransactionType.PaymentReceived, 5000m));

        Assert.Equal(0m, balance);
    }

    [Fact]
    public void Adjustment_AppliesItsSign()
    {
        var balance = Balance(
            Entry(TransactionType.Lent, 1000m),
            Entry(TransactionType.Adjustment, -100m));

        Assert.Equal(900m, balance);
    }
}