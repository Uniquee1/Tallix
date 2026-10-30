using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Sync;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly GetSnapshot _getSnapshot;

    public SyncController(GetSnapshot getSnapshot)
    {
        _getSnapshot = getSnapshot;
    }

    /// <param name="after">The nextCursor from the previous page. Leave out for the first page.</param>
    /// <param name="limit">Rows per page: 1 to 1000, default 500.</param>
    [HttpGet("snapshot/contacts")]
    public async Task<ActionResult<SnapshotPageDto<ContactDto>>> Contacts(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        return Ok(await _getSnapshot.ContactsAsync(after, limit, cancellationToken));
    }

    [HttpGet("snapshot/entries")]
    public async Task<ActionResult<SnapshotPageDto<LedgerEntryDto>>> Entries(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        return Ok(await _getSnapshot.EntriesAsync(after, limit, cancellationToken));
    }
}