using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class LedgerEntryTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();

    private static LedgerEntry Entry(
        TransactionType type, decimal amount, string? description = null) =>
        LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, type, Money.Php(amount),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, description);

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var entry = Entry(TransactionType.Lent, 5000m, "  Rent help  ");

        Assert.Equal(UserId, entry.UserId);
        Assert.Equal(ContactId, entry.ContactId);
        Assert.Equal(TransactionType.Lent, entry.Type);
        Assert.Equal(Money.Php(5000m), entry.Amount);
        Assert.Equal("Rent help", entry.Description);
    }

    [Theory]
    [InlineData(TransactionType.Lent, 0)]
    [InlineData(TransactionType.Lent, -5)]
    [InlineData(TransactionType.Borrowed, -1)]
    [InlineData(TransactionType.PaymentReceived, 0)]
    [InlineData(TransactionType.PaymentMade, -10)]
    public void Create_RejectsZeroOrNegativeAmount_ForNonAdjustment(
        TransactionType type, int amount)
    {
        Assert.Throws<DomainException>(() => Entry(type, amount));
    }

    [Fact]
    public void Create_Adjustment_AllowsNegativeAmount()
    {
        var entry = Entry(TransactionType.Adjustment, -250m);

        Assert.Equal(-250m, entry.SignedAmount.Amount);
    }

    [Fact]
    public void Create_Adjustment_RejectsZero()
    {
        Assert.Throws<DomainException>(() => Entry(TransactionType.Adjustment, 0m));
    }

    [Fact]
    public void Create_RejectsTooLongDescription()
    {
        var tooLong = new string('x', LedgerEntry.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() => Entry(TransactionType.Lent, 100m, tooLong));
    }

    [Fact]
    public void Create_RejectsEmptyContactId()
    {
        Assert.Throws<DomainException>(() =>
            LedgerEntry.Create(
                Guid.NewGuid(), UserId, Guid.Empty, TransactionType.Lent,
                Money.Php(100m), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(TransactionType.Lent, 100)]
    [InlineData(TransactionType.PaymentMade, 100)]
    [InlineData(TransactionType.Borrowed, -100)]
    [InlineData(TransactionType.PaymentReceived, -100)]
    public void SignedAmount_FollowsTheSignConvention(TransactionType type, int expected)
    {
        var entry = Entry(type, 100m);

        Assert.Equal((decimal)expected, entry.SignedAmount.Amount);
    }
}