namespace Tallix.Domain.Ledger;

public sealed record DashboardSummary(
    Money TotalOwedToMe,
    Money TotalIOwe,
    int ActiveContactCount,
    IReadOnlyList<OutstandingDebt> Overdue,
    IReadOnlyList<OutstandingDebt> Upcoming)
{
    public Money NetBalance => TotalOwedToMe.Subtract(TotalIOwe);
}

public static class DashboardCalculator
{
    public const int UpcomingDays = 7;

    /// <summary>
    /// Works out the dashboard figures from ALL of a user's entries.
    /// Balances are netted within a contact (BR-006) but never across contacts.
    /// </summary>
    public static DashboardSummary Calculate(
        IEnumerable<LedgerEntry> entries, DateOnly asOf, Currency currency)
    {
        var owedToMe = Money.Zero(currency);
        var iOwe = Money.Zero(currency);
        var activeContacts = 0;
        var overdue = new List<OutstandingDebt>();
        var upcoming = new List<OutstandingDebt>();

        // Guard against asOf near the end of the calendar, where adding days would throw.
        var horizon = asOf > DateOnly.MaxValue.AddDays(-UpcomingDays)
            ? DateOnly.MaxValue
            : asOf.AddDays(UpcomingDays);

        foreach (var group in entries.GroupBy(e => e.ContactId))
        {
            var contactEntries = group.ToList();
            var balance = BalanceCalculator.Calculate(contactEntries, currency);

            // BR-007: a zero balance means nothing outstanding, so the contact isn't active.
            if (balance.Amount == 0)
                continue;

            activeContacts++;

            if (balance.Amount > 0)
                owedToMe = owedToMe.Add(balance);
            else
                iOwe = iOwe.Add(balance.Negate());

            foreach (var debt in OutstandingDebtCalculator.Calculate(contactEntries, currency))
            {
                if (debt.Entry.DueDate is not { } dueDate)
                    continue;

                if (dueDate < asOf)
                    overdue.Add(debt);
                else if (dueDate <= horizon)
                    upcoming.Add(debt);
            }
        }

        return new DashboardSummary(
            owedToMe, iOwe, activeContacts, SortByDueDate(overdue), SortByDueDate(upcoming));
    }

    private static IReadOnlyList<OutstandingDebt> SortByDueDate(List<OutstandingDebt> debts) =>
        debts.OrderBy(d => d.Entry.DueDate).ThenBy(d => d.Entry.Id).ToList();
}