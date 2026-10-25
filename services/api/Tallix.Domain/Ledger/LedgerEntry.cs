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

    /// <summary>
    /// If this entry reverses another one, the id of the entry it reverses.
    /// </summary>
    public Guid? ReversesEntryId { get; }

    public bool IsReversal => ReversesEntryId is not null;

    // For EF Core materialization only. Application code must use Create(...).
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid id, Guid userId, Guid contactId, TransactionType type,
        Money amount, string? description, DateTimeOffset occurredAt,
        DateOnly? dueDate, DateTimeOffset createdAt, Guid? reversesEntryId)
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
        ReversesEntryId = reversesEntryId;
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
        DateOnly? dueDate = null) =>
        Build(id, userId, contactId, type, amount, occurredAt, createdAt,
            description, dueDate, reversesEntryId: null);

    /// <summary>
    /// Builds the entry that cancels out <paramref name="original"/>: an Adjustment whose
    /// amount is the exact opposite of the original's effect on the balance.
    /// </summary>
    public static LedgerEntry CreateReversal(
        Guid id,
        LedgerEntry original,
        DateTimeOffset now,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (original.IsReversal)
            throw new DomainException(
                "A reversal cannot be reversed. Record a new entry instead.");

        return Build(
            id,
            original.UserId,
            original.ContactId,
            TransactionType.Adjustment,
            original.SignedAmount.Negate(),
            now,
            now,
            description,
            dueDate: null,
            reversesEntryId: original.Id);
    }

    private static LedgerEntry Build(
        Guid id,
        Guid userId,
        Guid contactId,
        TransactionType type,
        Money amount,
        DateTimeOffset occurredAt,
        DateTimeOffset createdAt,
        string? description,
        DateOnly? dueDate,
        Guid? reversesEntryId)
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

        // The same instant, expressed with offset zero. PostgreSQL's timestamptz
        // (through Npgsql) only accepts UTC, and a phone may send +08:00.
        return new LedgerEntry(
            id, userId, contactId, type, amount,
            trimmedDescription,
            occurredAt.ToUniversalTime(),
            dueDate,
            createdAt.ToUniversalTime(),
            reversesEntryId);
    }
}