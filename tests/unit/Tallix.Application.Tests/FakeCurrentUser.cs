using Tallix.Application.Users;

namespace Tallix.Application.Tests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public UserDto User { get; set; }

    public FakeCurrentUser(UserDto user)
    {
        User = user;
    }

    public static UserDto NewUser(string name) =>
        new(Guid.NewGuid(), $"{name.ToLowerInvariant()}@example.com", name);

    public Task<UserDto> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(User);
}