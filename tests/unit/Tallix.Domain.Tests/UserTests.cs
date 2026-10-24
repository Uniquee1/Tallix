using Tallix.Domain.Common;
using Tallix.Domain.Users;

namespace Tallix.Domain.Tests;

public class UserTests
{
    private const string Subject = "auth0|abc123";

    [Fact]
    public void Create_NormalizesEmailAndTrimsName()
    {
        var user = User.Create(
            Guid.NewGuid(), Subject, "  Marc@Example.COM ", "  Marc  ", DateTimeOffset.UtcNow);

        Assert.Equal("marc@example.com", user.Email);
        Assert.Equal("Marc", user.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Create_RejectsInvalidEmail(string email)
    {
        Assert.Throws<DomainException>(() =>
            User.Create(Guid.NewGuid(), Subject, email, "Marc", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsBlankDisplayName()
    {
        Assert.Throws<DomainException>(() =>
            User.Create(Guid.NewGuid(), Subject, "marc@example.com", "  ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_KeepsAuthSubjectExactlyAsGiven()
    {
        var user = User.Create(
            Guid.NewGuid(), "Auth0|AbC123", "marc@example.com", "Marc", DateTimeOffset.UtcNow);

        Assert.Equal("Auth0|AbC123", user.AuthSubject);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankAuthSubject(string subject)
    {
        Assert.Throws<DomainException>(() =>
            User.Create(Guid.NewGuid(), subject, "marc@example.com", "Marc", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsTooLongAuthSubject()
    {
        var tooLong = new string('x', User.MaxAuthSubjectLength + 1);

        Assert.Throws<DomainException>(() =>
            User.Create(Guid.NewGuid(), tooLong, "marc@example.com", "Marc", DateTimeOffset.UtcNow));
    }
}