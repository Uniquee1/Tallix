using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Ledger;

namespace Tallix.Api.Controllers;

[ApiController]
[Authorize]
public class LedgerController : ControllerBase
{
    private readonly RecordLedgerEntry _recordLedgerEntry;
    private readonly GetLedgerEntry _getLedgerEntry;
    private readonly GetContactLedger _getContactLedger;
    private readonly ReverseLedgerEntry _reverseLedgerEntry;

    public LedgerController(
        RecordLedgerEntry recordLedgerEntry,
        GetLedgerEntry getLedgerEntry,
        GetContactLedger getContactLedger,
        ReverseLedgerEntry reverseLedgerEntry)
    {
        _recordLedgerEntry = recordLedgerEntry;
        _getLedgerEntry = getLedgerEntry;
        _getContactLedger = getContactLedger;
        _reverseLedgerEntry = reverseLedgerEntry;
    }

    [HttpPost("api/contacts/{contactId:guid}/ledger")]
    public async Task<ActionResult<LedgerEntryDto>> Record(
        Guid contactId,
        RecordLedgerEntryRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _recordLedgerEntry.ExecuteAsync(contactId, request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    [HttpGet("api/contacts/{contactId:guid}/ledger")]
    public async Task<ActionResult<ContactLedgerDto>> GetHistory(
        Guid contactId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetContactLedger.DefaultPageSize)
    {
        return Ok(await _getContactLedger.ExecuteAsync(contactId, page, pageSize, cancellationToken));
    }

    [HttpGet("api/ledger/{id:guid}")]
    public async Task<ActionResult<LedgerEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _getLedgerEntry.ExecuteAsync(id, cancellationToken));
    }

    [HttpPost("api/ledger/{id:guid}/reverse")]
    public async Task<ActionResult<LedgerEntryDto>> Reverse(
        Guid id,
        ReverseLedgerEntryRequest? request,
        CancellationToken cancellationToken)
    {
        var reversal = await _reverseLedgerEntry.ExecuteAsync(id, request?.Description, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = reversal.Id }, reversal);
    }
}