using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class LedgerEntryTimeTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();

    [Fact]
    public void Create_ConvertsOccurredAtToUtc_KeepingTheSameInstant()
    {
        // 14:30 in the Philippines is 06:30 UTC.
        var philippineTime = new DateTimeOffset(2026, 10, 23, 14, 30, 0, TimeSpan.FromHours(8));

        var entry = LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, TransactionType.Lent, Money.Php(100m),
            philippineTime, DateTimeOffset.UtcNow);

        Assert.Equal(TimeSpan.Zero, entry.OccurredAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 23, 6, 30, 0, TimeSpan.Zero), entry.OccurredAt);
    }

    [Fact]
    public void Create_ConvertsCreatedAtToUtc()
    {
        var philippineTime = new DateTimeOffset(2026, 10, 23, 14, 30, 0, TimeSpan.FromHours(8));

        var entry = LedgerEntry.Create(
            Guid.NewGuid(), UserId, ContactId, TransactionType.Lent, Money.Php(100m),
            DateTimeOffset.UtcNow, philippineTime);

        Assert.Equal(TimeSpan.Zero, entry.CreatedAt.Offset);
    }
}