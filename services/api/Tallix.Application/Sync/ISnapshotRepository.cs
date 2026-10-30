using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Sync;

public interface ISnapshotRepository
{
    /// <summary>
    /// The user's contacts with an id strictly greater than <paramref name="after"/>,
    /// ordered by id, at most <paramref name="take"/> of them.
    /// </summary>
    Task<IReadOnlyList<Contact>> ListContactsAfterAsync(
        Guid userId, Guid after, int take, CancellationToken cancellationToken);

    /// <summary>
    /// The user's ledger entries with an id strictly greater than <paramref name="after"/>,
    /// ordered by id, at most <paramref name="take"/> of them.
    /// </summary>
    Task<IReadOnlyList<LedgerEntry>> ListEntriesAfterAsync(
        Guid userId, Guid after, int take, CancellationToken cancellationToken);
}