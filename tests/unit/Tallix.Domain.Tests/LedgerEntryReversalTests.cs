using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class LedgerEntryReversalTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();

    private static LedgerEntry Entry(TransactionType type, decimal amount) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, type, Money.Php(amount),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static LedgerEntry ReverseOf(LedgerEntry original) =>
        LedgerEntry.CreateReversal(Guid.NewGuid(), original, DateTimeOffset.UtcNow);

    private static decimal Balance(params LedgerEntry[] entries) =>
        BalanceCalculator.Calculate(entries, Currency.PHP).Amount;

    [Fact]
    public void Reversal_OfLent_NetsToZero()
    {
        var lent = Entry(TransactionType.Lent, 5000m);

        Assert.Equal(0m, Balance(lent, ReverseOf(lent)));
    }

    [Fact]
    public void Reversal_OfBorrowed_NetsToZero()
    {
        var borrowed = Entry(TransactionType.Borrowed, 1000m);

        Assert.Equal(0m, Balance(borrowed, ReverseOf(borrowed)));
    }

    [Fact]
    public void Reversal_OfAnAdjustment_NetsToZero()
    {
        var adjustment = Entry(TransactionType.Adjustment, -250m);

        Assert.Equal(0m, Balance(adjustment, ReverseOf(adjustment)));
    }

    [Fact]
    public void Reversal_LinksBack_AndCopiesOwnerAndContact()
    {
        var original = Entry(TransactionType.Lent, 5000m);

        var reversal = ReverseOf(original);

        Assert.True(reversal.IsReversal);
        Assert.Equal(original.Id, reversal.ReversesEntryId);
        Assert.Equal(TransactionType.Adjustment, reversal.Type);
        Assert.Equal(original.UserId, reversal.UserId);
        Assert.Equal(original.ContactId, reversal.ContactId);
        Assert.Null(reversal.DueDate);
    }

    [Fact]
    public void Reversal_OfAReversal_IsRejected()
    {
        var reversal = ReverseOf(Entry(TransactionType.Lent, 5000m));

        Assert.Throws<DomainException>(() => ReverseOf(reversal));
    }

    [Fact]
    public void NormalEntry_IsNotAReversal()
    {
        var entry = Entry(TransactionType.Lent, 100m);

        Assert.False(entry.IsReversal);
        Assert.Null(entry.ReversesEntryId);
    }
}