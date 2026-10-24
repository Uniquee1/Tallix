using Tallix.Application.Common;
using Tallix.Application.Users;

namespace Tallix.Application.Contacts;

public sealed class DeleteContact
{
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;

    public DeleteContact(IContactRepository contacts, ICurrentUser currentUser)
    {
        _contacts = contacts;
        _currentUser = currentUser;
    }

    public async Task ExecuteAsync(Guid contactId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        var contact = await _contacts.GetByIdAsync(user.Id, contactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        if (!await _contacts.TryDeleteAsync(contact, cancellationToken))
            throw new ConflictException("This contact has transactions and cannot be deleted.");
    }
}