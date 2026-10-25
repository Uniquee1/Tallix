using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Dashboard;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GetDashboard _getDashboard;

    public DashboardController(GetDashboard getDashboard)
    {
        _getDashboard = getDashboard;
    }

    /// <param name="asOf">
    /// The caller's local date, like 2026-10-25. Due dates are judged against it.
    /// Defaults to today's date in UTC.
    /// </param>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(
        [FromQuery] DateOnly? asOf,
        CancellationToken cancellationToken)
    {
        return Ok(await _getDashboard.ExecuteAsync(asOf, cancellationToken));
    }
}