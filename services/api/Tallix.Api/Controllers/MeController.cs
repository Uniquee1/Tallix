using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tallix.Application.Users;

namespace Tallix.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    // Must match the namespace used by the Auth0 Action. It is only an identifier.
    private const string ClaimNamespace = "https://api.tallix.dev/";

    private readonly GetOrCreateCurrentUser _getOrCreateCurrentUser;

    public MeController(GetOrCreateCurrentUser getOrCreateCurrentUser)
    {
        _getOrCreateCurrentUser = getOrCreateCurrentUser;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
            return Unauthorized();

        var request = new CurrentUserRequest(
            subject,
            User.FindFirst(ClaimNamespace + "email")?.Value,
            User.FindFirst(ClaimNamespace + "name")?.Value);

        var user = await _getOrCreateCurrentUser.ExecuteAsync(request, cancellationToken);

        return Ok(user);
    }
}