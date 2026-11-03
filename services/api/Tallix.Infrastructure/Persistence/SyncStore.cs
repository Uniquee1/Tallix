using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tallix.Application.Common;
using Tallix.Application.Sync;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Infrastructure.Persistence;

internal sealed class SyncStore : ISyncStore
{
    private const string OperationKey = "pk_sync_operations";
    private const string ContactKey = "pk_contacts";
    private const string ReversalIndex = "ux_ledger_entries_reverses_entry_id";

    private readonly TallixDbContext _db;

    public SyncStore(TallixDbContext db)
    {
        _db = db;
    }

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await work();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            // Forget everything this attempt loaded or changed, so the next operation
            // starts from what is really in the database.
            _db.ChangeTracker.Clear();
        }
    }

    // ---------- Processed operations ----------

    public async Task<StoredOperation?> FindOperationAsync(
        Guid userId, Guid operationId, CancellationToken cancellationToken)
    {
        var row = await _db.Set<SyncOperationRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Id == operationId, cancellationToken);

        return row is null
            ? null
            : new StoredOperation(
                row.Id, row.OperationType, row.EntityType, row.EntityId, row.Status, row.Code, row.Message);
    }

    public async Task RecordOperationAsync(
        Guid userId, StoredOperation operation, CancellationToken cancellationToken)
    {
        _db.Set<SyncOperationRecord>().Add(new SyncOperationRecord
        {
            UserId = userId,
            Id = operation.Id,
            OperationType = operation.OperationType,
            EntityType = operation.EntityType,
            EntityId = operation.EntityId,
            Status = operation.Status,
            Code = operation.Code,
            Message = operation.Message is { Length: > 500 } message ? message[..500] : operation.Message
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.UniqueViolation, OperationKey))
        {
            throw new DuplicateOperationException();
        }
    }

    // ---------- Contacts ----------

    // Tracked, so a later SaveChanges knows which version the edit started from.
    public Task<Contact?> FindContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        _db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId, cancellationToken);

    public Task<bool> ContactIdTakenAsync(Guid contactId, CancellationToken cancellationToken) =>
        _db.Contacts.AnyAsync(c => c.Id == contactId, cancellationToken);

    public async Task AddContactAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Add(contact);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.UniqueViolation, ContactKey))
        {
            throw new ConflictException("That contact id is already in use.");
        }
    }

    public async Task SaveContactChangesAsync(Contact contact, CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This contact was changed somewhere else since your edit.");
        }
    }

    public Task<bool> ContactHasEntriesAsync(Guid contactId, CancellationToken cancellationToken) =>
        _db.LedgerEntries.AnyAsync(e => e.ContactId == contactId, cancellationToken);

    public async Task DeleteContactAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Remove(contact);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.ForeignKeyViolation))
        {
            throw new ConflictException("This contact has transactions and cannot be deleted.");
        }
    }

    // ---------- Ledger entries ----------

    public Task<LedgerEntry?> FindEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        _db.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId, cancellationToken);

    public Task<bool> EntryIdTakenAsync(Guid entryId, CancellationToken cancellationToken) =>
        _db.LedgerEntries.AnyAsync(e => e.Id == entryId, cancellationToken);

    public Task<bool> IsReversedAsync(Guid entryId, CancellationToken cancellationToken) =>
        _db.LedgerEntries.AnyAsync(e => e.ReversesEntryId == entryId, cancellationToken);

    public async Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        _db.LedgerEntries.Add(entry);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.UniqueViolation, ReversalIndex))
        {
            throw new ConflictException("This entry has already been reversed.");
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.ForeignKeyViolation))
        {
            // The contact was deleted between our check and this insert.
            throw new NotFoundException("Contact not found.");
        }
    }

    private static bool IsViolation(DbUpdateException ex, string sqlState, string? constraint = null) =>
        ex.InnerException is PostgresException pg &&
        pg.SqlState == sqlState &&
        (constraint is null || pg.ConstraintName == constraint);
}