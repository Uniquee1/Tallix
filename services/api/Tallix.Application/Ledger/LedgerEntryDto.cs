using Tallix.Domain.Ledger;

namespace Tallix.Application.Ledger;

public sealed record LedgerEntryDto(
    Guid Id,
    Guid ContactId,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    decimal SignedAmount,
    string? Description,
    DateTimeOffset OccurredAt,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    Guid? ReversesEntryId)
{
    public static LedgerEntryDto From(LedgerEntry entry) => new(
        entry.Id,
        entry.ContactId,
        entry.Type,
        entry.Amount.Amount,
        entry.Amount.Currency,
        entry.SignedAmount.Amount,
        entry.Description,
        entry.OccurredAt,
        entry.DueDate,
        entry.CreatedAt,
        entry.ReversesEntryId);
}