using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Users;
using Tallix.Domain.Common;

namespace Tallix.Application.Sync;

public sealed record SnapshotPageDto<T>(IReadOnlyList<T> Items, string? NextCursor);

public sealed class GetSnapshot
{
    public const int DefaultLimit = 500;
    public const int MaxLimit = 1000;

    private readonly ISnapshotRepository _snapshot;
    private readonly ICurrentUser _currentUser;

    public GetSnapshot(ISnapshotRepository snapshot, ICurrentUser currentUser)
    {
        _snapshot = snapshot;
        _currentUser = currentUser;
    }

    public async Task<SnapshotPageDto<ContactDto>> ContactsAsync(
        string? after, int? limit, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);
        var take = ClampLimit(limit);

        // One extra row tells us whether another page exists, without a second query.
        var rows = await _snapshot.ListContactsAfterAsync(
            user.Id, ParseCursor(after), take + 1, cancellationToken);

        return ToPage(rows, take, row => row.Id, ContactDto.From);
    }

    public async Task<SnapshotPageDto<LedgerEntryDto>> EntriesAsync(
        string? after, int? limit, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);
        var take = ClampLimit(limit);

        var rows = await _snapshot.ListEntriesAfterAsync(
            user.Id, ParseCursor(after), take + 1, cancellationToken);

        return ToPage(rows, take, row => row.Id, LedgerEntryDto.From);
    }

    private static int ClampLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

    // No cursor means "from the beginning". No row has the empty id, so every row sorts after it.
    private static Guid ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return Guid.Empty;

        if (!Guid.TryParse(cursor, out var id))
            throw new DomainException("The cursor is not valid.");

        return id;
    }

    private static SnapshotPageDto<TDto> ToPage<TRow, TDto>(
        IReadOnlyList<TRow> rows,
        int take,
        Func<TRow, Guid> idOf,
        Func<TRow, TDto> toDto)
    {
        var hasMore = rows.Count > take;
        var page = rows.Take(take).ToList();

        return new SnapshotPageDto<TDto>(
            page.Select(toDto).ToList(),
            hasMore ? idOf(page[^1]).ToString() : null);
    }
}