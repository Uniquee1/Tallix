namespace Tallix.Domain.Ledger;

public sealed record RunningBalanceEntry(LedgerEntry Entry, Money Balance);

public static class BalanceCalculator
{
    public static Money Calculate(IEnumerable<LedgerEntry> entries, Currency currency)
    {
        var balance = Money.Zero(currency);

        foreach (var entry in entries)
        {
            balance = balance.Add(entry.SignedAmount);
        }

        return balance;
    }

    /// <summary>
    /// Each entry together with the balance right after it, oldest first.
    /// The order is part of the definition: by when it happened, then by when it
    /// was recorded, then by id so the result is always the same.
    /// </summary>
    public static IReadOnlyList<RunningBalanceEntry> WithRunningBalance(
        IEnumerable<LedgerEntry> entries, Currency currency)
    {
        var balance = Money.Zero(currency);
        var result = new List<RunningBalanceEntry>();

        foreach (var entry in entries
                     .OrderBy(e => e.OccurredAt)
                     .ThenBy(e => e.CreatedAt)
                     .ThenBy(e => e.Id))
        {
            balance = balance.Add(entry.SignedAmount);
            result.Add(new RunningBalanceEntry(entry, balance));
        }

        return result;
    }
}