using Tallix.Domain.Ledger;

namespace Tallix.Application.Ledger;

public interface ILedgerRepository
{
    /// <summary>
    /// Finds an entry by id, but only if it belongs to the given user.
    /// There is deliberately no way to look up an entry without a user id.
    /// </summary>
    Task<LedgerEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new entry. Returns false (and saves nothing) if its contact no longer
    /// exists, for example because it was deleted a moment ago.
    /// </summary>
    Task<bool> TryAddAsync(LedgerEntry entry, CancellationToken cancellationToken);
}