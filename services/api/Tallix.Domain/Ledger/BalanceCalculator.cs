namespace Tallix.Domain.Ledger;

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
}