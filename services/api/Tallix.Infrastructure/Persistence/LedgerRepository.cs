using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tallix.Application.Ledger;
using Tallix.Domain.Ledger;

namespace Tallix.Infrastructure.Persistence;

internal sealed class LedgerRepository : ILedgerRepository
{
    // The unique index from the AddReversalLinkToLedgerEntries migration.
    private const string ReversalUniqueIndex = "ux_ledger_entries_reverses_entry_id";

    private readonly TallixDbContext _db;

    public LedgerRepository(TallixDbContext db)
    {
        _db = db;
    }

    public Task<LedgerEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        _db.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> ListByContactAsync(
        Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.ContactId == contactId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryAddAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        _db.LedgerEntries.Add(entry);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // The contact was deleted between our check and this insert.
            _db.Entry(entry).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryAddReversalAsync(LedgerEntry reversal, CancellationToken cancellationToken)
    {
        _db.LedgerEntries.Add(reversal);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ReversalUniqueIndex
            })
        {
            // Someone already reversed this entry. Forget the rejected row.
            _db.Entry(reversal).State = EntityState.Detached;
            return false;
        }
    }
}