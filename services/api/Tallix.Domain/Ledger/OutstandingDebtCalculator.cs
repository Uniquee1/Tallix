using Tallix.Domain.Common;

namespace Tallix.Domain.Ledger;

public enum DebtDirection
{
    OwedToMe,
    IOwe
}

public sealed record OutstandingDebt(LedgerEntry Entry, Money Outstanding, DebtDirection Direction);

public static class OutstandingDebtCalculator
{
    /// <summary>
    /// What is still outstanding for ONE contact, given all of that contact's entries.
    /// Payments are applied to dated debts first (oldest due date first), then to undated ones.
    /// </summary>
    public static IReadOnlyList<OutstandingDebt> Calculate(
        IReadOnlyCollection<LedgerEntry> entries, Currency currency)
    {
        if (entries.Select(e => e.ContactId).Distinct().Count() > 1)
            throw new DomainException("Outstanding debts are worked out for one contact at a time.");

        var balance = BalanceCalculator.Calculate(entries, currency);

        if (balance.Amount == 0)
            return Array.Empty<OutstandingDebt>();

        var direction = balance.Amount > 0 ? DebtDirection.OwedToMe : DebtDirection.IOwe;
        var debtType = direction == DebtDirection.OwedToMe
            ? TransactionType.Lent
            : TransactionType.Borrowed;

        // A reversed entry is cancelled by its reversal, so it is no longer a debt.
        var reversedIds = entries
            .Where(e => e.ReversesEntryId is not null)
            .Select(e => e.ReversesEntryId!.Value)
            .ToHashSet();

        var debts = entries
            .Where(e => e.Type == debtType && !reversedIds.Contains(e.Id))
            .OrderBy(e => e.DueDate is null)      // dated debts first
            .ThenBy(e => e.DueDate)               // then oldest due date first
            .ThenBy(e => e.OccurredAt)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToList();

        // Whatever the balance doesn't account for has been settled by payments and the like.
        var settled = Math.Max(debts.Sum(d => d.Amount.Amount) - Math.Abs(balance.Amount), 0m);

        var result = new List<OutstandingDebt>();

        foreach (var debt in debts)
        {
            var absorbed = Math.Min(debt.Amount.Amount, settled);
            settled -= absorbed;

            var outstanding = debt.Amount.Amount - absorbed;

            if (outstanding > 0)
                result.Add(new OutstandingDebt(debt, new Money(outstanding, currency), direction));
        }

        return result;
    }
}