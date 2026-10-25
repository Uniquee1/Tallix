using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tallix.Application.Ledger;
using Tallix.Domain.Ledger;

namespace Tallix.Infrastructure.Persistence;

internal sealed class LedgerRepository : ILedgerRepository
{
    private readonly TallixDbContext _db;

    public LedgerRepository(TallixDbContext db)
    {
        _db = db;
    }

    public Task<LedgerEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        _db.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId, cancellationToken);

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
}