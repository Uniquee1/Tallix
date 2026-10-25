using Tallix.Application.Common;
using Tallix.Application.Ledger;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class GetLedgerEntryTests
{
    private readonly InMemoryLedgerRepository _ledger = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly GetLedgerEntry _get;

    public GetLedgerEntryTests()
    {
        _get = new GetLedgerEntry(_ledger, _currentUser);
    }

    private LedgerEntry SeedEntry(Guid userId)
    {
        var entry = LedgerEntry.Create(
            Guid.NewGuid(), userId, Guid.NewGuid(), TransactionType.Lent,
            Money.Php(100m), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _ledger.Seed(entry);
        return entry;
    }

    [Fact]
    public async Task Get_ReturnsYourOwnEntry()
    {
        var entry = SeedEntry(_currentUser.User.Id);

        var dto = await _get.ExecuteAsync(entry.Id, CancellationToken.None);

        Assert.Equal(entry.Id, dto.Id);
    }

    [Fact]
    public async Task Get_UnknownId_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _get.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Get_AnotherUsersEntry_IsNotFound()
    {
        var theirs = SeedEntry(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(
            () => _get.ExecuteAsync(theirs.Id, CancellationToken.None));
    }
}