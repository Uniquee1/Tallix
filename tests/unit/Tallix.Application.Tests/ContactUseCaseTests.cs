using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Tests;

public class ContactUseCaseTests
{
    private readonly InMemoryContactRepository _repo = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly CreateContact _create;
    private readonly GetContact _get;

    public ContactUseCaseTests()
    {
        _create = new CreateContact(_repo, _currentUser, TimeProvider.System);
        _get = new GetContact(_repo, _currentUser);
    }

    private Task<ContactDto> Create(string name) =>
        _create.ExecuteAsync(
            new CreateContactRequest(name, null, null, null), CancellationToken.None);

    [Fact]
    public async Task Create_StoresTheContactForTheCurrentUser()
    {
        var dto = await _create.ExecuteAsync(
            new CreateContactRequest("  Juan  ", "0917", null, null), CancellationToken.None);

        Assert.Equal("Juan", dto.Name);

        var stored = Assert.Single(_repo.Contacts);
        Assert.Equal(_currentUser.User.Id, stored.UserId);
    }

    [Fact]
    public async Task Create_WithBlankName_IsRejected_AndStoresNothing()
    {
        await Assert.ThrowsAsync<DomainException>(() => Create("   "));

        Assert.Empty(_repo.Contacts);
    }

    [Fact]
    public async Task Get_ReturnsYourOwnContact()
    {
        var created = await Create("Juan");

        var found = await _get.ExecuteAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, found.Id);
    }

    [Fact]
    public async Task Get_UnknownId_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _get.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Get_AnotherUsersContact_IsNotFound()
    {
        var someoneElsesContact = Contact.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Secret", DateTimeOffset.UtcNow);
        _repo.Seed(someoneElsesContact);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _get.ExecuteAsync(someoneElsesContact.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ContactCreatedByAlice_CannotBeSeenByBob_ButStillByAlice()
    {
        var aliceContact = await Create("Juan");

        _currentUser.User = FakeCurrentUser.NewUser("Bob");

        await Assert.ThrowsAsync<NotFoundException>(
            () => _get.ExecuteAsync(aliceContact.Id, CancellationToken.None));
    }
}