using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class LedgerEntryTypeTests
{
    [Fact]
    public void Create_RejectsUndefinedTransactionType()
    {
        Assert.Throws<DomainException>(() => LedgerEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (TransactionType)99,
            Money.Php(100m), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }
}