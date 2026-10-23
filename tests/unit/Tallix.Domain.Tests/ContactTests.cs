using Tallix.Domain.Common;
using Tallix.Domain.Contacts;

namespace Tallix.Domain.Tests;

public class ContactTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_TrimsName()
    {
        var contact = Contact.Create(
            Guid.NewGuid(), UserId, "  Juan Santos  ", DateTimeOffset.UtcNow);

        Assert.Equal("Juan Santos", contact.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.Throws<DomainException>(() =>
            Contact.Create(Guid.NewGuid(), UserId, name, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsEmptyUserId()
    {
        Assert.Throws<DomainException>(() =>
            Contact.Create(Guid.NewGuid(), Guid.Empty, "Juan", DateTimeOffset.UtcNow));
    }
}