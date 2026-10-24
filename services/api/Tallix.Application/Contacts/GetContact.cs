using Tallix.Application.Common;
using Tallix.Application.Users;

namespace Tallix.Application.Contacts;

public sealed class GetContact
{
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;

    public GetContact(IContactRepository contacts, ICurrentUser currentUser)
    {
        _contacts = contacts;
        _currentUser = currentUser;
    }

    public async Task<ContactDto> ExecuteAsync(Guid contactId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        // The same answer whether the contact doesn't exist or belongs to someone else.
        var contact = await _contacts.GetByIdAsync(user.Id, contactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        return ContactDto.From(contact);
    }
}