using Tallix.Domain.Contacts;

namespace Tallix.Application.Contacts;

public sealed record ContactDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ContactDto From(Contact contact) => new(
        contact.Id,
        contact.Name,
        contact.Phone,
        contact.Email,
        contact.Notes,
        contact.CreatedAt,
        contact.UpdatedAt);
}