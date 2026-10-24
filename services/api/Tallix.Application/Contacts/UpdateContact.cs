using Tallix.Application.Common;
using Tallix.Application.Users;

namespace Tallix.Application.Contacts;

public sealed record UpdateContactRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Notes);

public sealed class UpdateContact
{
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public UpdateContact(
        IContactRepository contacts,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _contacts = contacts;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ContactDto> ExecuteAsync(
        Guid contactId,
        UpdateContactRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        var contact = await _contacts.GetByIdAsync(user.Id, contactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        contact.Update(
            request.Name, request.Phone, request.Email, request.Notes, _clock.GetUtcNow());

        await _contacts.UpdateAsync(contact, cancellationToken);

        return ContactDto.From(contact);
    }
}