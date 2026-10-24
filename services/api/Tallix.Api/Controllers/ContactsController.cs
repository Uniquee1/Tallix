using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Contacts;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly CreateContact _createContact;
    private readonly GetContact _getContact;

    public ContactsController(CreateContact createContact, GetContact getContact)
    {
        _createContact = createContact;
        _getContact = getContact;
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create(
        CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        var contact = await _createContact.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _getContact.ExecuteAsync(id, cancellationToken));
    }
}