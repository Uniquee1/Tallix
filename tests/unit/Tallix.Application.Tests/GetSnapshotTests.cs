using Tallix.Application.Sync;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class GetSnapshotTests
{
    private readonly InMemorySnapshotRepository _repo = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly GetSnapshot _snapshot;

    public GetSnapshotTests()
    {
        _snapshot = new GetSnapshot(_repo, _currentUser);
    }

    private void SeedContacts(Guid userId, int count)
    {
        for (var i = 0; i < count; i++)
            _repo.Contacts.Add(Contact.Create(Guid.NewGuid(), userId, $"C{i}", DateTimeOffset.UtcNow));
    }

    private void SeedEntries(Guid userId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _repo.Entries.Add(LedgerEntry.Create(
                Guid.NewGuid(), userId, Guid.NewGuid(), TransactionType.Lent,
                Money.Php(100m), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        }
    }

    // Walks every page the way the phone will.
    private async Task<List<Guid>> AllContactIds(int limit)
    {
        var ids = new List<Guid>();
        string? cursor = null;

        do
        {
            var page = await _snapshot.ContactsAsync(cursor, limit, CancellationToken.None);
            ids.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        return ids;
    }

    private async Task<List<Guid>> AllEntryIds(int limit)
    {
        var ids = new List<Guid>();
        string? cursor = null;

        do
        {
            var page = await _snapshot.EntriesAsync(cursor, limit, CancellationToken.None);
            ids.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        return ids;
    }

    [Fact]
    public async Task Contacts_PagingReturnsEveryRowExactlyOnce_InOrder()
    {
        SeedContacts(_currentUser.User.Id, 7);
        var expected = _repo.Contacts.Select(c => c.Id).OrderBy(id => id).ToList();

        var actual = await AllContactIds(limit: 3);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Contacts_WhenTheLastPageIsExactlyFull_ThereIsNoNextCursor()
    {
        SeedContacts(_currentUser.User.Id, 4);

        var first = await _snapshot.ContactsAsync(null, 2, CancellationToken.None);
        var second = await _snapshot.ContactsAsync(first.NextCursor, 2, CancellationToken.None);

        Assert.Equal(2, first.Items.Count);
        Assert.NotNull(first.NextCursor);
        Assert.Equal(2, second.Items.Count);
        Assert.Null(second.NextCursor);
    }

    [Fact]
    public async Task Contacts_OnlyIncludesTheCurrentUsersRows()
    {
        SeedContacts(_currentUser.User.Id, 2);
        SeedContacts(Guid.NewGuid(), 3);

        var ids = await AllContactIds(limit: 10);

        Assert.Equal(2, ids.Count);
        Assert.All(ids, id => Assert.Equal(_currentUser.User.Id, _repo.Contacts.Single(c => c.Id == id).UserId));
    }

    [Fact]
    public async Task Entries_PagingReturnsEveryRowExactlyOnce_InOrder()
    {
        SeedEntries(_currentUser.User.Id, 5);
        var expected = _repo.Entries.Select(e => e.Id).OrderBy(id => id).ToList();

        var actual = await AllEntryIds(limit: 2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Entries_OnlyIncludesTheCurrentUsersRows()
    {
        SeedEntries(_currentUser.User.Id, 2);
        SeedEntries(Guid.NewGuid(), 4);

        var ids = await AllEntryIds(limit: 10);

        Assert.Equal(2, ids.Count);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task AnInvalidCursor_IsRejected(string cursor)
    {
        await Assert.ThrowsAsync<DomainException>(
            () => _snapshot.ContactsAsync(cursor, 10, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ATooSmallLimit_IsRaisedToOne(int limit)
    {
        SeedContacts(_currentUser.User.Id, 3);

        var page = await _snapshot.ContactsAsync(null, limit, CancellationToken.None);

        Assert.Single(page.Items);
        Assert.NotNull(page.NextCursor);
    }

    [Fact]
    public async Task WithNoRows_TheFirstPageIsEmpty_AndHasNoCursor()
    {
        var page = await _snapshot.EntriesAsync(null, null, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Null(page.NextCursor);
    }
}