using Tallix.Domain.Common;
using Tallix.Domain.Contacts;

namespace Tallix.Domain.Tests;

public class ContactTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Contact NewContact(DateTimeOffset? createdAt = null) =>
        Contact.Create(Guid.NewGuid(), UserId, "Juan", createdAt ?? DateTimeOffset.UtcNow);

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

    [Fact]
    public void Create_SetsUpdatedAtEqualToCreatedAt()
    {
        var created = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        var contact = NewContact(created);

        Assert.Equal(created, contact.CreatedAt);
        Assert.Equal(created, contact.UpdatedAt);
    }

    [Fact]
    public void Create_BlankOptionalFields_BecomeNull()
    {
        var contact = Contact.Create(
            Guid.NewGuid(), UserId, "Juan", DateTimeOffset.UtcNow,
            phone: "  ", email: "", notes: " ");

        Assert.Null(contact.Phone);
        Assert.Null(contact.Email);
        Assert.Null(contact.Notes);
    }

    [Fact]
    public void Create_RejectsTooLongOptionalFields()
    {
        Assert.Throws<DomainException>(() => Contact.Create(
            Guid.NewGuid(), UserId, "Juan", DateTimeOffset.UtcNow,
            phone: new string('1', Contact.MaxPhoneLength + 1)));

        Assert.Throws<DomainException>(() => Contact.Create(
            Guid.NewGuid(), UserId, "Juan", DateTimeOffset.UtcNow,
            email: new string('e', Contact.MaxEmailLength + 1)));

        Assert.Throws<DomainException>(() => Contact.Create(
            Guid.NewGuid(), UserId, "Juan", DateTimeOffset.UtcNow,
            notes: new string('n', Contact.MaxNotesLength + 1)));
    }

    [Fact]
    public void Update_ChangesDetailsAndUpdatedAt_ButNotCreatedAt()
    {
        var created = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var later = created.AddDays(1);
        var contact = NewContact(created);

        contact.Update("  Juan Santos ", "0917", "juan@example.com", "Neighbor", later);

        Assert.Equal("Juan Santos", contact.Name);
        Assert.Equal("0917", contact.Phone);
        Assert.Equal("juan@example.com", contact.Email);
        Assert.Equal("Neighbor", contact.Notes);
        Assert.Equal(later, contact.UpdatedAt);
        Assert.Equal(created, contact.CreatedAt);
    }

    [Fact]
    public void Update_WithBlankName_IsRejected_AndLeavesContactUnchanged()
    {
        var created = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var contact = NewContact(created);

        Assert.Throws<DomainException>(() =>
            contact.Update("   ", "0917", null, null, created.AddDays(1)));

        Assert.Equal("Juan", contact.Name);
        Assert.Null(contact.Phone);
        Assert.Equal(created, contact.UpdatedAt);
    }
}