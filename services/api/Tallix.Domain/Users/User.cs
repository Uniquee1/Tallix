using Tallix.Domain.Common;

namespace Tallix.Domain.Users;

public class User
{
    public const int MaxEmailLength = 320;
    public const int MaxDisplayNameLength = 200;
    public const int MaxAuthSubjectLength = 255;

    public Guid Id { get; }
    public string AuthSubject { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public DateTimeOffset CreatedAt { get; }

    // For EF Core materialization only. Application code must use Create(...).
    private User()
    {
        AuthSubject = null!;
        Email = null!;
        DisplayName = null!;
    }

    private User(
        Guid id, string authSubject, string email,
        string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        AuthSubject = authSubject;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public static User Create(
        Guid id,
        string authSubject,
        string email,
        string displayName,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new DomainException("User id is required.");

        // The subject is an opaque ID from the identity provider.
        // We validate it but never modify it (no trimming, no lowercasing).
        if (string.IsNullOrWhiteSpace(authSubject))
            throw new DomainException("Auth subject is required.");

        if (authSubject.Length > MaxAuthSubjectLength)
            throw new DomainException(
                $"Auth subject cannot exceed {MaxAuthSubjectLength} characters.");

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

        return new User(id, authSubject, normalizedEmail, trimmedName, createdAt);
    }
}