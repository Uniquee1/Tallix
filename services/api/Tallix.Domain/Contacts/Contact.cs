using Tallix.Domain.Common;

namespace Tallix.Domain.Contacts;

public class Contact
{
    public const int MaxNameLength = 200;

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; }
    public string? Phone { get; }
    public string? Email { get; }
    public string? Notes { get; }
    public DateTimeOffset CreatedAt { get; }

    private Contact()
    {
        Name = null!;
    }

    private Contact(
        Guid id, Guid userId, string name,
        string? phone, string? email, string? notes,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Phone = phone;
        Email = email;
        Notes = notes;
        CreatedAt = createdAt;
    }

    public static Contact Create(
        Guid id,
        Guid userId,
        string name,
        DateTimeOffset createdAt,
        string? phone = null,
        string? email = null,
        string? notes = null)
    {
        if (id == Guid.Empty)
            throw new DomainException("Contact id is required.");

        if (userId == Guid.Empty)
            throw new DomainException("A contact must belong to a user.");

        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
            throw new DomainException("Contact name is required.");

        if (trimmedName.Length > MaxNameLength)
            throw new DomainException($"Contact name cannot exceed {MaxNameLength} characters.");

        return new Contact(id, userId, trimmedName, phone, email, notes, createdAt);
    }
}