using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Tests;

public class UpdateAndDeleteContactTests
{
    private readonly InMemoryContactRepository _repo = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly FixedTimeProvider _clock =
        new(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly UpdateContact _update;
    private readonly DeleteContact _delete;

    public UpdateAndDeleteContactTests()
    {
        _update = new UpdateContact(_repo, _currentUser, _clock);
        _delete = new DeleteContact(_repo, _currentUser);
    }

    private Contact SeedOwn(string name = "Juan")
    {
        var contact = Contact.Create(
            Guid.NewGuid(), _currentUser.User.Id, name, _clock.GetUtcNow());
        _repo.Seed(contact);
        return contact;
    }

    private Contact SeedForSomeoneElse()
    {
        var contact = Contact.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Secret", _clock.GetUtcNow());
        _repo.Seed(contact);
        return contact;
    }

    [Fact]
    public async Task Update_ChangesTheDetails_AndMovesUpdatedAtForward()
    {
        var contact = SeedOwn();
        _clock.Now = _clock.Now.AddDays(1);

        var dto = await _update.ExecuteAsync(
            contact.Id,
            new UpdateContactRequest("Juan Santos", "0917", null, "Neighbor"),
            CancellationToken.None);

        Assert.Equal("Juan Santos", dto.Name);
        Assert.Equal("0917", dto.Phone);
        Assert.Equal(_clock.Now, dto.UpdatedAt);
        Assert.NotEqual(dto.CreatedAt, dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_AnotherUsersContact_IsNotFound_AndUnchanged()
    {
        var theirs = SeedForSomeoneElse();

        await Assert.ThrowsAsync<NotFoundException>(() => _update.ExecuteAsync(
            theirs.Id,
            new UpdateContactRequest("Hacked", null, null, null),
            CancellationToken.None));

        Assert.Equal("Secret", theirs.Name);
    }

    [Fact]
    public async Task Update_WithBlankName_IsRejected_AndLeavesTheContactUnchanged()
    {
        var contact = SeedOwn("Juan");

        await Assert.ThrowsAsync<DomainException>(() => _update.ExecuteAsync(
            contact.Id,
            new UpdateContactRequest("  ", "0917", null, null),
            CancellationToken.None));

        Assert.Equal("Juan", contact.Name);
        Assert.Null(contact.Phone);
    }

    [Fact]
    public async Task Delete_RemovesYourContact()
    {
        var contact = SeedOwn();

        await _delete.ExecuteAsync(contact.Id, CancellationToken.None);

        Assert.Empty(_repo.Contacts);
    }

    [Fact]
    public async Task Delete_AnotherUsersContact_IsNotFound_AndNotDeleted()
    {
        var theirs = SeedForSomeoneElse();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _delete.ExecuteAsync(theirs.Id, CancellationToken.None));

        Assert.Single(_repo.Contacts);
    }

    [Fact]
    public async Task Delete_ContactWithTransactions_IsAConflict_AndNotDeleted()
    {
        var contact = SeedOwn();
        _repo.MarkAsHavingEntries(contact.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _delete.ExecuteAsync(contact.Id, CancellationToken.None));

        Assert.Single(_repo.Contacts);
    }
}