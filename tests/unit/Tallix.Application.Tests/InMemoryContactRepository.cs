using Tallix.Application.Contacts;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Tests;

internal sealed class InMemoryContactRepository : IContactRepository
{
    private readonly List<Contact> _contacts = new();

    public IReadOnlyList<Contact> Contacts => _contacts;

    public void Seed(Contact contact) => _contacts.Add(contact);

    // Mirrors the real repository: a contact is only found if it belongs to the user.
    public Task<Contact?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(_contacts.FirstOrDefault(c => c.Id == contactId && c.UserId == userId));

    public Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _contacts.Add(contact);
        return Task.CompletedTask;
    }
}