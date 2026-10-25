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
    /// All of one contact's entries, but only if they belong to the given user.
    /// No particular order: the domain orders them.
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> ListByContactAsync(
        Guid userId, Guid contactId, CancellationToken cancellationToken);

    /// <summary>
    /// All of the user's entries, across every contact. No particular order.
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new entry. Returns false (and saves nothing) if its contact no longer
    /// exists, for example because it was deleted a moment ago.
    /// </summary>
    Task<bool> TryAddAsync(LedgerEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a reversal. Returns false (and saves nothing) if the entry it reverses
    /// has already been reversed.
    /// </summary>
    Task<bool> TryAddReversalAsync(LedgerEntry reversal, CancellationToken cancellationToken);
}