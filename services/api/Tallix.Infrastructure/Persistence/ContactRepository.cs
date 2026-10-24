using Microsoft.EntityFrameworkCore;
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

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }
}