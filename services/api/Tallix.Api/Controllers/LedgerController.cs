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

    public LedgerController(RecordLedgerEntry recordLedgerEntry, GetLedgerEntry getLedgerEntry)
    {
        _recordLedgerEntry = recordLedgerEntry;
        _getLedgerEntry = getLedgerEntry;
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

    [HttpGet("api/ledger/{id:guid}")]
    public async Task<ActionResult<LedgerEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _getLedgerEntry.ExecuteAsync(id, cancellationToken));
    }
}