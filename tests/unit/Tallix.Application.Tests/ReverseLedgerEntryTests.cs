using Tallix.Application.Common;
using Tallix.Application.Ledger;
using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class ReverseLedgerEntryTests
{
    private readonly InMemoryLedgerRepository _ledger = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly FixedTimeProvider _clock =
        new(new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero));
    private readonly ReverseLedgerEntry _reverse;

    public ReverseLedgerEntryTests()
    {
        _reverse = new ReverseLedgerEntry(_ledger, _currentUser, _clock);
    }

    private LedgerEntry SeedEntry(Guid userId, decimal amount = 5000m)
    {
        var entry = LedgerEntry.Create(
            Guid.NewGuid(), userId, Guid.NewGuid(), TransactionType.Lent,
            Money.Php(amount), _clock.Now.AddDays(-1), _clock.Now.AddDays(-1));
        _ledger.Seed(entry);
        return entry;
    }

    private Task<LedgerEntryDto> Reverse(Guid entryId, string? description = null) =>
        _reverse.ExecuteAsync(entryId, description, CancellationToken.None);

    [Fact]
    public async Task Reverse_RecordsTheOppositeEntry_AndTheBalanceNetsToZero()
    {
        var original = SeedEntry(_currentUser.User.Id, 5000m);

        var dto = await Reverse(original.Id);

        Assert.Equal(-5000m, dto.SignedAmount);
        Assert.Equal(0m, BalanceCalculator.Calculate(_ledger.Entries, Currency.PHP).Amount);
    }

    [Fact]
    public async Task Reverse_LinksToTheOriginal_AndKeepsTheDescription()
    {
        var original = SeedEntry(_currentUser.User.Id);

        var dto = await Reverse(original.Id, "Entered by mistake");

        Assert.Equal(original.Id, dto.ReversesEntryId);
        Assert.Equal(TransactionType.Adjustment, dto.Type);
        Assert.Equal("Entered by mistake", dto.Description);
        Assert.Equal(_clock.Now, dto.OccurredAt);
    }

    [Fact]
    public async Task Reverse_AnotherUsersEntry_IsNotFound_AndStoresNothing()
    {
        var theirs = SeedEntry(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => Reverse(theirs.Id));

        Assert.Single(_ledger.Entries);
    }

    [Fact]
    public async Task Reverse_AReversal_IsRejected()
    {
        var original = SeedEntry(_currentUser.User.Id);
        var reversal = await Reverse(original.Id);

        await Assert.ThrowsAsync<DomainException>(() => Reverse(reversal.Id));

        Assert.Equal(2, _ledger.Entries.Count);
    }

    [Fact]
    public async Task Reverse_TheSameEntryTwice_IsAConflict()
    {
        var original = SeedEntry(_currentUser.User.Id);
        await Reverse(original.Id);

        await Assert.ThrowsAsync<ConflictException>(() => Reverse(original.Id));

        Assert.Equal(2, _ledger.Entries.Count);
    }
}