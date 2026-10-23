using Tallix.Domain.Common;

namespace Tallix.Domain.Users;

public class User
{
    public const int MaxEmailLength = 320;
    public const int MaxDisplayNameLength = 200;

    public Guid Id { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public DateTimeOffset CreatedAt { get; }

    // For EF Core materialization only. Application code must use Create(...).
    private User()
    {
        Email = null!;
        DisplayName = null!;
    }

    private User(Guid id, string email, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public static User Create(
        Guid id,
        string email,
        string displayName,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new DomainException("User id is required.");

        var normalizedEmail = email?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedEmail) || !normalizedEmail.Contains('@'))
            throw new DomainException("A valid email is required.");

        if (normalizedEmail.Length > MaxEmailLength)
            throw new DomainException($"Email cannot exceed {MaxEmailLength} characters.");

        var trimmedName = displayName?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
            throw new DomainException("Display name is required.");

        if (trimmedName.Length > MaxDisplayNameLength)
            throw new DomainException(
                $"Display name cannot exceed {MaxDisplayNameLength} characters.");

        return new User(id, normalizedEmail, trimmedName, createdAt);
    }
}