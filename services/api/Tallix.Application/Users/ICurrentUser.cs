namespace Tallix.Application.Users;

public interface ICurrentUser
{
    /// <summary>
    /// The Tallix user making the current request.
    /// </summary>
    Task<UserDto> GetAsync(CancellationToken cancellationToken);
}