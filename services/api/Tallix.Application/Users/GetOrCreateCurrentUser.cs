using Tallix.Application.Common;
using Tallix.Domain.Users;

namespace Tallix.Application.Users;

public sealed record CurrentUserRequest(string AuthSubject, string? Email, string? Name);

public sealed record UserDto(Guid Id, string Email, string DisplayName);

public sealed class GetOrCreateCurrentUser
{
    private readonly IUserRepository _users;
    private readonly TimeProvider _clock;

    public GetOrCreateCurrentUser(IUserRepository users, TimeProvider clock)
    {
        _users = users;
        _clock = clock;
    }

    public async Task<UserDto> ExecuteAsync(
        CurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _users.FindByAuthSubjectAsync(request.AuthSubject, cancellationToken);

        if (existing is not null)
            return ToDto(existing);

        var user = User.Create(
            Guid.NewGuid(),
            request.AuthSubject,
            request.Email ?? string.Empty,
            DisplayNameFor(request),
            _clock.GetUtcNow());

        if (await _users.TryAddAsync(user, cancellationToken))
            return ToDto(user);

        // The insert was rejected by a unique index. Either another request created
        // this same user a moment ago (fine, load it), or a different account
        // already owns this email (a real conflict).
        var created = await _users.FindByAuthSubjectAsync(request.AuthSubject, cancellationToken);

        if (created is not null)
            return ToDto(created);

        throw new ConflictException("Another account already uses this email address.");
    }

    private static string DisplayNameFor(CurrentUserRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
            return request.Name;

        var email = request.Email ?? string.Empty;
        var at = email.IndexOf('@');

        return at > 0 ? email[..at] : email;
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName);
}