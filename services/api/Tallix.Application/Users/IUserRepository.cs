using Tallix.Domain.Users;

namespace Tallix.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByAuthSubjectAsync(string authSubject, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new user. Returns false (and saves nothing) if a uniqueness rule
    /// rejected it: the auth subject or the email already exists.
    /// </summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);
}