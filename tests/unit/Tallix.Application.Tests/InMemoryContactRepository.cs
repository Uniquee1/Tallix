using Tallix.Application.Contacts;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Tests;

internal sealed class InMemoryContactRepository : IContactRepository
{
    private readonly List<Contact> _contacts = new();
    private readonly HashSet<Guid> _contactsWithEntries = new();

    public IReadOnlyList<Contact> Contacts => _contacts;

    public void Seed(Contact contact) => _contacts.Add(contact);

    /// <summary>
    /// Pretends this contact has ledger entries, so deleting it is refused,
    /// like the foreign key in the real database.
    /// </summary>
    public void MarkAsHavingEntries(Guid contactId) => _contactsWithEntries.Add(contactId);

    // Mirrors the real repository: a contact is only found if it belongs to the user.
    public Task<Contact?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(_contacts.FirstOrDefault(c => c.Id == contactId && c.UserId == userId));

    public Task<ContactPage> ListAsync(
        Guid userId, string? search, int skip, int take, CancellationToken cancellationToken)
    {
        var matches = _contacts.Where(c => c.UserId == userId);

        if (search is not null)
        {
            matches = matches.Where(c =>
                Contains(c.Name, search) || Contains(c.Email, search) || Contains(c.Phone, search));
        }

        var all = matches
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Id)
            .ToList();

        return Task.FromResult(new ContactPage(all.Skip(skip).Take(take).ToList(), all.Count));
    }

    public Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _contacts.Add(contact);
        return Task.CompletedTask;
    }

    // The contact object was already changed in place, so there is nothing to save.
    public Task UpdateAsync(Contact contact, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<bool> TryDeleteAsync(Contact contact, CancellationToken cancellationToken)
    {
        if (_contactsWithEntries.Contains(contact.Id))
            return Task.FromResult(false);

        _contacts.Remove(contact);
        return Task.FromResult(true);
    }

    private static bool Contains(string? value, string search) =>
        value is not null && value.Contains(search, StringComparison.OrdinalIgnoreCase);
}