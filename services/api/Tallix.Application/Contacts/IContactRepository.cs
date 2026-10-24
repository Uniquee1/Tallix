using Tallix.Domain.Contacts;

namespace Tallix.Application.Contacts;

public interface IContactRepository
{
    /// <summary>
    /// Finds a contact by id, but only if it belongs to the given user.
    /// There is deliberately no way to look up a contact without a user id.
    /// </summary>
    Task<Contact?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken);

    Task AddAsync(Contact contact, CancellationToken cancellationToken);
}