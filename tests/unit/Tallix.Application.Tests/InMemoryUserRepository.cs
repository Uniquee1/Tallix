using Tallix.Application.Users;
using Tallix.Domain.Users;

namespace Tallix.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IReadOnlyList<User> Users => _users;
    public int AddAttempts { get; private set; }

    /// <summary>
    /// If set, the next add simulates losing a race: this user "was created by
    /// another request just before ours", and our insert is rejected.
    /// </summary>
    public User? RaceWinner { get; set; }

    public void Seed(User user) => _users.Add(user);

    public Task<User?> FindByAuthSubjectAsync(string authSubject, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.AuthSubject == authSubject));

    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        AddAttempts++;

        if (RaceWinner is not null)
        {
            _users.Add(RaceWinner);
            RaceWinner = null;
            return Task.FromResult(false);
        }

        // Mirrors the two unique indexes in the real database.
        if (_users.Any(u => u.AuthSubject == user.AuthSubject || u.Email == user.Email))
            return Task.FromResult(false);

        _users.Add(user);
        return Task.FromResult(true);
    }
}