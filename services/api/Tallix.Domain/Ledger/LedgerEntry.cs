using Tallix.Domain.Common;

namespace Tallix.Domain.Ledger;

public class LedgerEntry
{
    public const int MaxDescriptionLength = 500;

    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid ContactId { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }
    public string? Description { get; }
    public DateTimeOffset OccurredAt { get; }
    public DateOnly? DueDate { get; }
    public DateTimeOffset CreatedAt { get; }

    private LedgerEntry(
        Guid id, Guid userId, Guid contactId, TransactionType type,
        Money amount, string? description, DateTimeOffset occurredAt,
        DateOnly? dueDate, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        ContactId = contactId;
        Type = type;
        Amount = amount;
        Description = description;
        OccurredAt = occurredAt;
        DueDate = dueDate;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// The entry's effect on the contact's balance.
    /// Positive = contact owes the user more. Negative = the user owes the contact more.
    /// </summary>
    public Money SignedAmount => Type switch
    {
        TransactionType.Lent => Amount,
        TransactionType.PaymentMade => Amount,
        TransactionType.Borrowed => Amount.Negate(),
        TransactionType.PaymentReceived => Amount.Negate(),
        TransactionType.Adjustment => Amount,
        _ => throw new DomainException($"Unknown transaction type: {Type}.")
    };

    public static LedgerEntry Create(
        Guid id,
        Guid userId,
        Guid contactId,
        TransactionType type,
        Money amount,
        DateTimeOffset occurredAt,
        DateTimeOffset createdAt,
        string? description = null,
        DateOnly? dueDate = null)
    {
        if (id == Guid.Empty)
            throw new DomainException("Ledger entry id is required.");

        if (userId == Guid.Empty)
            throw new DomainException("A ledger entry must belong to a user.");

        if (contactId == Guid.Empty)
            throw new DomainException("A ledger entry must belong to a contact.");

        if (type == TransactionType.Adjustment)
        {
            if (amount.Amount == 0)
                throw new DomainException("An adjustment cannot be zero.");
        }
        else if (amount.Amount <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        var trimmedDescription = description?.Trim();

        if (trimmedDescription is { Length: > MaxDescriptionLength })
            throw new DomainException(
                $"Description cannot exceed {MaxDescriptionLength} characters.");

        return new LedgerEntry(
            id, userId, contactId, type, amount,
            trimmedDescription, occurredAt, dueDate, createdAt);
    }
}