using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Sync;

/// <summary>What the server remembers about an operation it has already handled.</summary>
public sealed record StoredOperation(
    Guid Id,
    string OperationType,
    string EntityType,
    Guid EntityId,
    string Status,
    string? Code,
    string? Message);

/// <summary>Another request recorded this same operation id first.</summary>
public sealed class DuplicateOperationException : Exception
{
    public DuplicateOperationException() : base("This operation was already recorded.")
    {
    }
}

/// <summary>
/// Everything the sync path needs from the database. Its methods throw the Application's own
/// exceptions for expected violations (NotFound, Conflict). They never swallow a database
/// error, because inside a PostgreSQL transaction a failed statement poisons the whole
/// transaction. The caller rolls back instead.
/// </summary>
public interface ISyncStore
{
    /// <summary>Runs the work in one transaction: committed if it returns, rolled back if it throws.</summary>
    Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken);

    Task<StoredOperation?> FindOperationAsync(Guid userId, Guid operationId, CancellationToken cancellationToken);

    /// <summary>Throws DuplicateOperationException if the id is already recorded.</summary>
    Task RecordOperationAsync(Guid userId, StoredOperation operation, CancellationToken cancellationToken);

    Task<Contact?> FindContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken);

    /// <summary>True if ANY user already has a contact with this id.</summary>
    Task<bool> ContactIdTakenAsync(Guid contactId, CancellationToken cancellationToken);

    Task AddContactAsync(Contact contact, CancellationToken cancellationToken);

    /// <summary>Saves a contact loaded through FindContactAsync. Conflict if its version moved meanwhile.</summary>
    Task SaveContactChangesAsync(Contact contact, CancellationToken cancellationToken);

    Task<bool> ContactHasEntriesAsync(Guid contactId, CancellationToken cancellationToken);

    Task DeleteContactAsync(Contact contact, CancellationToken cancellationToken);

    Task<LedgerEntry?> FindEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);

    /// <summary>True if ANY user already has an entry with this id.</summary>
    Task<bool> EntryIdTakenAsync(Guid entryId, CancellationToken cancellationToken);

    Task<bool> IsReversedAsync(Guid entryId, CancellationToken cancellationToken);

    Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken);
}