using Microsoft.EntityFrameworkCore;
using Tallix.Application.Sync;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Infrastructure.Persistence;

internal sealed class SnapshotRepository : ISnapshotRepository
{
    private readonly TallixDbContext _db;

    public SnapshotRepository(TallixDbContext db)
    {
        _db = db;
    }

    // LINQ can't compare two Guids with ">", so these are written as SQL.
    // FromSqlInterpolated turns every {value} into a query PARAMETER, never into pasted text.
    public async Task<IReadOnlyList<Contact>> ListContactsAfterAsync(
        Guid userId, Guid after, int take, CancellationToken cancellationToken) =>
        await _db.Contacts
            .FromSqlInterpolated($@"
                SELECT * FROM contacts
                WHERE user_id = {userId} AND id > {after}
                ORDER BY id
                LIMIT {take}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> ListEntriesAfterAsync(
    Guid userId, Guid after, int take, CancellationToken cancellationToken) =>
    await _db.LedgerEntries
        .FromSqlInterpolated($@"
            SELECT
                id,
                user_id,
                contact_id,
                type,
                amount AS ""Amount_Amount"",
                currency AS ""Amount_Currency"",
                description,
                occurred_at,
                due_date,
                created_at,
                reverses_entry_id
            FROM ledger_entries
            WHERE user_id = {userId} AND id > {after}
            ORDER BY id
            LIMIT {take}")
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}