using Tallix.Application.Common;
using Tallix.Application.Users;
using Tallix.Domain.Common;
using Tallix.Domain.Users;

namespace Tallix.Application.Tests;

public class GetOrCreateCurrentUserTests
{
    private readonly InMemoryUserRepository _repo = new();
    private readonly GetOrCreateCurrentUser _useCase;

    public GetOrCreateCurrentUserTests()
    {
        _useCase = new GetOrCreateCurrentUser(_repo, TimeProvider.System);
    }

    private Task<UserDto> Run(string subject, string? email, string? name) =>
        _useCase.ExecuteAsync(new CurrentUserRequest(subject, email, name), CancellationToken.None);

    private static User ExistingUser(string subject, string email) =>
        User.Create(Guid.NewGuid(), subject, email, "Existing", DateTimeOffset.UtcNow);

    [Fact]
    public async Task FirstRequest_CreatesTheUser()
    {
        var result = await Run("auth0|abc", "Marc@Example.com", "Marc");

        Assert.Equal("marc@example.com", result.Email);
        Assert.Equal("Marc", result.DisplayName);
        Assert.Single(_repo.Users);
        Assert.Equal("auth0|abc", _repo.Users[0].AuthSubject);
    }

    [Fact]
    public async Task SecondRequest_ReturnsTheSameUser_AndCreatesNothingNew()
    {
        var first = await Run("auth0|abc", "marc@example.com", "Marc");
        var second = await Run("auth0|abc", "marc@example.com", "Marc");

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_repo.Users);
        Assert.Equal(1, _repo.AddAttempts);
    }

    [Fact]
    public async Task MissingName_UsesThePartOfTheEmailBeforeTheAt()
    {
        var result = await Run("auth0|abc", "marc@example.com", null);

        Assert.Equal("marc", result.DisplayName);
    }

    [Fact]
    public async Task NewUserWithoutEmail_IsRejected_AndNothingIsStored()
    {
        await Assert.ThrowsAsync<DomainException>(() => Run("auth0|abc", null, "Marc"));

        Assert.Empty(_repo.Users);
    }

    [Fact]
    public async Task ExistingUser_DoesNotNeedAnEmailClaim()
    {
        var existing = ExistingUser("auth0|abc", "marc@example.com");
        _repo.Seed(existing);

        var result = await Run("auth0|abc", null, null);

        Assert.Equal(existing.Id, result.Id);
    }

    [Fact]
    public async Task LostRace_ReturnsTheUserTheOtherRequestCreated()
    {
        var winner = ExistingUser("auth0|abc", "marc@example.com");
        _repo.RaceWinner = winner;

        var result = await Run("auth0|abc", "marc@example.com", "Marc");

        Assert.Equal(winner.Id, result.Id);
        Assert.Single(_repo.Users);
    }

    [Fact]
    public async Task EmailOwnedByAnotherAccount_IsAConflict()
    {
        _repo.Seed(ExistingUser("auth0|other", "marc@example.com"));

        await Assert.ThrowsAsync<ConflictException>(
            () => Run("auth0|abc", "marc@example.com", "Marc"));
    }
}