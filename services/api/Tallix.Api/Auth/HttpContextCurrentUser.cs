using Tallix.Application.Users;

namespace Tallix.Api.Auth;

internal sealed class HttpContextCurrentUser : ICurrentUser
{
    // Must match the namespace used by the Auth0 Action. It is only an identifier.
    private const string ClaimNamespace = "https://api.tallix.dev/";

    private readonly IHttpContextAccessor _http;
    private readonly GetOrCreateCurrentUser _getOrCreateCurrentUser;
    private UserDto? _user;

    public HttpContextCurrentUser(
        IHttpContextAccessor http,
        GetOrCreateCurrentUser getOrCreateCurrentUser)
    {
        _http = http;
        _getOrCreateCurrentUser = getOrCreateCurrentUser;
    }

    public async Task<UserDto> GetAsync(CancellationToken cancellationToken)
    {
        // Resolved once per request, then reused.
        if (_user is not null)
            return _user;

        var principal = _http.HttpContext?.User;
        var subject = principal?.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedAccessException("The access token has no subject.");

        var request = new CurrentUserRequest(
            subject,
            principal!.FindFirst(ClaimNamespace + "email")?.Value,
            principal.FindFirst(ClaimNamespace + "name")?.Value);

        _user = await _getOrCreateCurrentUser.ExecuteAsync(request, cancellationToken);

        return _user;
    }
}