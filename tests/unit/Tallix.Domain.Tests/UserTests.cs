using Tallix.Domain.Common;
using Tallix.Domain.Users;

namespace Tallix.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_NormalizesEmailAndTrimsName()
    {
        var user = User.Create(
            Guid.NewGuid(), "  Marc@Example.COM ", "  Marc  ", DateTimeOffset.UtcNow);

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
            User.Create(Guid.NewGuid(), email, "Marc", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsBlankDisplayName()
    {
        Assert.Throws<DomainException>(() =>
            User.Create(Guid.NewGuid(), "marc@example.com", "  ", DateTimeOffset.UtcNow));
    }
}