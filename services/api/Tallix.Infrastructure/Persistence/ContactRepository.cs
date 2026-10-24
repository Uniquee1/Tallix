using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tallix.Application.Contacts;
using Tallix.Domain.Contacts;

namespace Tallix.Infrastructure.Persistence;

internal sealed class ContactRepository : IContactRepository
{
    private readonly TallixDbContext _db;

    public ContactRepository(TallixDbContext db)
    {
        _db = db;
    }

    public Task<Contact?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId, cancellationToken);

    public async Task<ContactPage> ListAsync(
        Guid userId, string? search, int skip, int take, CancellationToken cancellationToken)
    {
        var query = _db.Contacts
            .AsNoTracking()
            .Where(c => c.UserId == userId);

        if (search is not null)
        {
            var pattern = $"%{EscapeLike(search)}%";

            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Email != null && EF.Functions.ILike(c.Email, pattern)) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new ContactPage(items, totalCount);
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Update(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryDeleteAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Remove(contact);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // A ledger entry still references this contact. Forget the failed delete.
            _db.Entry(contact).State = EntityState.Detached;
            return false;
        }
    }

    // In a LIKE pattern, % and _ are wildcards and \ is the escape character.
    // Without this, a user searching for "50%" would match far more than they asked for.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}