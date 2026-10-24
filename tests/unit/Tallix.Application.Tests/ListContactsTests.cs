using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Domain.Contacts;

namespace Tallix.Application.Tests;

public class ListContactsTests
{
    private readonly InMemoryContactRepository _repo = new();
    private readonly FakeCurrentUser _currentUser = new(FakeCurrentUser.NewUser("Alice"));
    private readonly ListContacts _list;

    public ListContactsTests()
    {
        _list = new ListContacts(_repo, _currentUser);
    }

    private void SeedFor(Guid userId, string name, string? phone = null, string? email = null) =>
        _repo.Seed(Contact.Create(
            Guid.NewGuid(), userId, name, DateTimeOffset.UtcNow, phone, email));

    private Task<PagedResult<ContactDto>> List(
        string? search = null, int page = 1, int pageSize = 20) =>
        _list.ExecuteAsync(new ListContactsRequest(search, page, pageSize), CancellationToken.None);

    [Fact]
    public async Task List_ReturnsOnlyTheCurrentUsersContacts()
    {
        SeedFor(_currentUser.User.Id, "Mine");
        SeedFor(Guid.NewGuid(), "Someone else's");

        var result = await List();

        var item = Assert.Single(result.Items);
        Assert.Equal("Mine", item.Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_IsOrderedByName()
    {
        SeedFor(_currentUser.User.Id, "Carlos");
        SeedFor(_currentUser.User.Id, "Ana");
        SeedFor(_currentUser.User.Id, "Ben");

        var result = await List();

        Assert.Equal(new[] { "Ana", "Ben", "Carlos" }, result.Items.Select(c => c.Name));
    }

    [Fact]
    public async Task List_Search_MatchesNameCaseInsensitively()
    {
        SeedFor(_currentUser.User.Id, "Juan Santos");
        SeedFor(_currentUser.User.Id, "Maria Cruz");

        var result = await List(search: "JUAN");

        var item = Assert.Single(result.Items);
        Assert.Equal("Juan Santos", item.Name);
    }

    [Fact]
    public async Task List_Search_MatchesPhoneAndEmail()
    {
        SeedFor(_currentUser.User.Id, "Ana", phone: "0917 111 2222");
        SeedFor(_currentUser.User.Id, "Ben", email: "ben@example.com");
        SeedFor(_currentUser.User.Id, "Carlos");

        var byPhone = await List(search: "0917");
        var byEmail = await List(search: "ben@");

        Assert.Equal("Ana", Assert.Single(byPhone.Items).Name);
        Assert.Equal("Ben", Assert.Single(byEmail.Items).Name);
    }

    [Fact]
    public async Task List_Paging_ReturnsTheRequestedPage_AndTheTotalOfAllMatches()
    {
        for (var i = 1; i <= 5; i++)
            SeedFor(_currentUser.User.Id, $"C{i}");

        var result = await List(page: 2, pageSize: 2);

        Assert.Equal(new[] { "C3", "C4" }, result.Items.Select(c => c.Name));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task List_ClampsPageAndPageSize()
    {
        var result = await List(page: 0, pageSize: 5000);

        Assert.Equal(1, result.Page);
        Assert.Equal(ListContacts.MaxPageSize, result.PageSize);
    }
}