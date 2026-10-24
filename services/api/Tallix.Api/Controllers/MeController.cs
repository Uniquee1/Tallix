using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            subject = User.FindFirst("sub")?.Value,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}