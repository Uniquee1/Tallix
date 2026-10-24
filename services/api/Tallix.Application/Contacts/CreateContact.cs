using Tallix.Application.Users;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Contacts;

public sealed record CreateContactRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Notes);

public sealed class CreateContact
{
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public CreateContact(
        IContactRepository contacts,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _contacts = contacts;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ContactDto> ExecuteAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        var contact = Contact.Create(
            Guid.NewGuid(),
            user.Id,
            request.Name,
            _clock.GetUtcNow(),
            request.Phone,
            request.Email,
            request.Notes);

        await _contacts.AddAsync(contact, cancellationToken);

        return ContactDto.From(contact);
    }
}