using Tallix.Domain.Contacts;

namespace Tallix.Application.Contacts;

public sealed record ContactPage(IReadOnlyList<Contact> Items, int TotalCount);

public interface IContactRepository
{
    /// <summary>
    /// Finds a contact by id, but only if it belongs to the given user.
    /// There is deliberately no way to look up a contact without a user id.
    /// </summary>
    Task<Contact?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken);

    /// <summary>
    /// One page of the user's contacts, optionally filtered by a search text
    /// (matched against name, email and phone), plus the total number of matches.
    /// </summary>
    Task<ContactPage> ListAsync(
        Guid userId, string? search, int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// All of the user's contacts, with no paging. Used where every name is needed.
    /// </summary>
    Task<IReadOnlyList<Contact>> ListAllAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Contact contact, CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes to a contact that was loaded through GetByIdAsync (so it is already
    /// known to belong to the current user).
    /// </summary>
    Task UpdateAsync(Contact contact, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the contact. Returns false (and deletes nothing) if the contact is still
    /// referenced, which means it has ledger entries.
    /// </summary>
    Task<bool> TryDeleteAsync(Contact contact, CancellationToken cancellationToken);
}