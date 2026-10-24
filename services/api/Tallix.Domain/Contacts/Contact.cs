using Tallix.Domain.Common;

namespace Tallix.Domain.Contacts;

public class Contact
{
    public const int MaxNameLength = 200;
    public const int MaxPhoneLength = 50;
    public const int MaxEmailLength = 320;
    public const int MaxNotesLength = 2000;

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // For EF Core materialization only. Application code must use Create(...).
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
        UpdatedAt = createdAt;
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

        var details = ValidateDetails(name, phone, email, notes);

        return new Contact(
            id, userId, details.Name, details.Phone, details.Email, details.Notes, createdAt);
    }

    public void Update(
        string name,
        string? phone,
        string? email,
        string? notes,
        DateTimeOffset updatedAt)
    {
        // Validate everything first. Only if all of it is valid do we change anything,
        // so a rejected update can never leave the contact half-modified.
        var details = ValidateDetails(name, phone, email, notes);

        Name = details.Name;
        Phone = details.Phone;
        Email = details.Email;
        Notes = details.Notes;
        UpdatedAt = updatedAt;
    }

    private static (string Name, string? Phone, string? Email, string? Notes) ValidateDetails(
        string name, string? phone, string? email, string? notes)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
            throw new DomainException("Contact name is required.");

        if (trimmedName.Length > MaxNameLength)
            throw new DomainException($"Contact name cannot exceed {MaxNameLength} characters.");

        return (
            trimmedName,
            Optional(phone, MaxPhoneLength, "Phone"),
            Optional(email, MaxEmailLength, "Email"),
            Optional(notes, MaxNotesLength, "Notes"));
    }

    // Optional text: trimmed, and blank becomes null so "no phone" is stored one way only.
    private static string? Optional(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }
}