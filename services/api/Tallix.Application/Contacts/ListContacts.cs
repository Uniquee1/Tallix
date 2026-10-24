using Tallix.Application.Common;
using Tallix.Application.Users;

namespace Tallix.Application.Contacts;

public sealed record ListContactsRequest(string? Search, int Page, int PageSize);

public sealed class ListContacts
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;

    public ListContacts(IContactRepository contacts, ICurrentUser currentUser)
    {
        _contacts = contacts;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ContactDto>> ExecuteAsync(
        ListContactsRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        // Computed as long so an absurd page number can't overflow into a negative offset.
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        var result = await _contacts.ListAsync(user.Id, search, skip, pageSize, cancellationToken);

        return new PagedResult<ContactDto>(
            result.Items.Select(ContactDto.From).ToList(),
            page,
            pageSize,
            result.TotalCount);
    }
}