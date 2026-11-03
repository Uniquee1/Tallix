using Tallix.Application.Common;
using Tallix.Application.Sync;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

// A fake that imitates the real store's behaviour, including rolling back a failed transaction.
// (Its snapshot is shallow: it can undo an added record, not a record edited in place.)
internal sealed class InMemorySyncStore : ISyncStore
{
    private (Guid UserId, StoredOperation Operation)? _committedElsewhere;

    public Dictionary<Guid, Contact> Contacts { get; private set; } = new();
    public Dictionary<Guid, LedgerEntry> Entries { get; private set; } = new();
    public List<(Guid UserId, StoredOperation Operation)> Operations { get; private set; } = new();

    /// <summary>Another request records this same operation just before ours does.</summary>
    public StoredOperation? ConcurrentWinner { get; set; }

    /// <summary>Another request finishes between our first look and our refusal.</summary>
    public StoredOperation? LateWinner { get; set; }

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        var contacts = new Dictionary<Guid, Contact>(Contacts);
        var entries = new Dictionary<Guid, LedgerEntry>(Entries);
        var operations = new List<(Guid, StoredOperation)>(Operations);

        try
        {
            return await work();
        }
        catch
        {
            Contacts = contacts;
            Entries = entries;
            Operations = operations;

            // What the other request committed survives our rollback.
            if (_committedElsewhere is { } committed)
            {
                Operations.Add(committed);
                _committedElsewhere = null;
            }

            throw;
        }
    }

    public Task<StoredOperation?> FindOperationAsync(Guid userId, Guid operationId, CancellationToken cancellationToken)
    {
        var found = Operations.FirstOrDefault(o => o.UserId == userId && o.Operation.Id == operationId).Operation;

        if (LateWinner is not null)
        {
            Operations.Add((userId, LateWinner));
            LateWinner = null;
        }

        return Task.FromResult<StoredOperation?>(found);
    }

    public Task RecordOperationAsync(Guid userId, StoredOperation operation, CancellationToken cancellationToken)
    {
        if (ConcurrentWinner is { } winner)
        {
            _committedElsewhere = (userId, winner);
            ConcurrentWinner = null;
            throw new DuplicateOperationException();
        }

        if (Operations.Any(o => o.UserId == userId && o.Operation.Id == operation.Id))
            throw new DuplicateOperationException();

        Operations.Add((userId, operation));
        return Task.CompletedTask;
    }

    public Task<Contact?> FindContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(Contacts.TryGetValue(contactId, out var c) && c.UserId == userId ? c : null);

    public Task<bool> ContactIdTakenAsync(Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(Contacts.ContainsKey(contactId));

    public Task AddContactAsync(Contact contact, CancellationToken cancellationToken)
    {
        Contacts[contact.Id] = contact;
        return Task.CompletedTask;
    }

    public Task SaveContactChangesAsync(Contact contact, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> ContactHasEntriesAsync(Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Values.Any(e => e.ContactId == contactId));

    public Task DeleteContactAsync(Contact contact, CancellationToken cancellationToken)
    {
        Contacts.Remove(contact.Id);
        return Task.CompletedTask;
    }

    public Task<LedgerEntry?> FindEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.TryGetValue(entryId, out var e) && e.UserId == userId ? e : null);

    public Task<bool> EntryIdTakenAsync(Guid entryId, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.ContainsKey(entryId));

    public Task<bool> IsReversedAsync(Guid entryId, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Values.Any(e => e.ReversesEntryId == entryId));

    public Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        if (entry.ReversesEntryId is { } original && Entries.Values.Any(e => e.ReversesEntryId == original))
            throw new ConflictException("This entry has already been reversed.");

        Entries[entry.Id] = entry;
        return Task.CompletedTask;
    }
}