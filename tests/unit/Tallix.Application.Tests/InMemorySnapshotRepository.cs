using Tallix.Application.Sync;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

internal sealed class InMemorySnapshotRepository : ISnapshotRepository
{
    public List<Contact> Contacts { get; } = new();
    public List<LedgerEntry> Entries { get; } = new();

    // Mirrors the real repository: this user's rows, strictly after the cursor, in id order.
    public Task<IReadOnlyList<Contact>> ListContactsAfterAsync(
        Guid userId, Guid after, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Contact>>(
            Contacts
                .Where(c => c.UserId == userId && c.Id.CompareTo(after) > 0)
                .OrderBy(c => c.Id)
                .Take(take)
                .ToList());

    public Task<IReadOnlyList<LedgerEntry>> ListEntriesAfterAsync(
        Guid userId, Guid after, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LedgerEntry>>(
            Entries
                .Where(e => e.UserId == userId && e.Id.CompareTo(after) > 0)
                .OrderBy(e => e.Id)
                .Take(take)
                .ToList());
}