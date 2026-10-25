using Tallix.Application.Ledger;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

internal sealed class InMemoryLedgerRepository : ILedgerRepository
{
    private readonly List<LedgerEntry> _entries = new();

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    /// <summary>
    /// If set, the next add is refused, like a foreign-key violation
    /// when the contact was deleted a moment ago.
    /// </summary>
    public bool RejectNextAdd { get; set; }

    public void Seed(LedgerEntry entry) => _entries.Add(entry);

    // Mirrors the real repository: an entry is only found if it belongs to the user.
    public Task<LedgerEntry?> GetByIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.FirstOrDefault(e => e.Id == entryId && e.UserId == userId));

    public Task<bool> TryAddAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        if (RejectNextAdd)
        {
            RejectNextAdd = false;
            return Task.FromResult(false);
        }

        _entries.Add(entry);
        return Task.FromResult(true);
    }
}