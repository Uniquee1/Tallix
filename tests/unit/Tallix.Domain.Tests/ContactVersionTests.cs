using Tallix.Domain.Common;
using Tallix.Domain.Contacts;

namespace Tallix.Domain.Tests;

public class ContactVersionTests
{
    private static Contact NewContact() =>
        Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "Juan", DateTimeOffset.UtcNow);

    [Fact]
    public void ANewContact_StartsAtVersionOne()
    {
        Assert.Equal(1, NewContact().Version);
    }

    [Fact]
    public void EachEdit_RaisesTheVersionByOne()
    {
        var contact = NewContact();

        contact.Update("Juan S.", null, null, null, DateTimeOffset.UtcNow);
        contact.Update("Juan Santos", null, null, null, DateTimeOffset.UtcNow);

        Assert.Equal(3, contact.Version);
    }

    [Fact]
    public void ARejectedEdit_DoesNotRaiseTheVersion()
    {
        var contact = NewContact();

        Assert.Throws<DomainException>(() =>
            contact.Update("  ", null, null, null, DateTimeOffset.UtcNow));

        Assert.Equal(1, contact.Version);
    }
}