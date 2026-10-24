using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Common;
using Tallix.Application.Contacts;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly ListContacts _listContacts;
    private readonly CreateContact _createContact;
    private readonly GetContact _getContact;
    private readonly UpdateContact _updateContact;
    private readonly DeleteContact _deleteContact;

    public ContactsController(
        ListContacts listContacts,
        CreateContact createContact,
        GetContact getContact,
        UpdateContact updateContact,
        DeleteContact deleteContact)
    {
        _listContacts = listContacts;
        _createContact = createContact;
        _getContact = getContact;
        _updateContact = updateContact;
        _deleteContact = deleteContact;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContactDto>>> List(
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListContacts.DefaultPageSize)
    {
        var result = await _listContacts.ExecuteAsync(
            new ListContactsRequest(search, page, pageSize), cancellationToken);

        return Ok(result);
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Update(
        Guid id,
        UpdateContactRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateContact.ExecuteAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteContact.ExecuteAsync(id, cancellationToken);

        return NoContent();
    }
}