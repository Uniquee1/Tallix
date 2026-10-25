using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Users;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Dashboard;

public sealed record DashboardDebtItemDto(
    Guid EntryId,
    Guid ContactId,
    string ContactName,
    DebtDirection Direction,
    decimal Outstanding,
    DateOnly DueDate);

public sealed record DashboardDueGroupDto(
    int Count,
    decimal OwedToMeTotal,
    decimal IOweTotal,
    IReadOnlyList<DashboardDebtItemDto> Items);

public sealed record DashboardTransactionDto(
    Guid EntryId,
    Guid ContactId,
    string ContactName,
    TransactionType Type,
    decimal Amount,
    decimal SignedAmount,
    DateTimeOffset OccurredAt,
    Guid? ReversesEntryId);

public sealed record DashboardDto(
    DateOnly AsOf,
    Currency Currency,
    decimal TotalOwedToMe,
    decimal TotalIOwe,
    decimal NetBalance,
    int ActiveContactCount,
    DashboardDueGroupDto Overdue,
    DashboardDueGroupDto Upcoming,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions);

public sealed class GetDashboard
{
    public const int MaxDueItems = 5;
    public const int RecentTransactionCount = 10;

    private readonly ILedgerRepository _ledger;
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public GetDashboard(
        ILedgerRepository ledger,
        IContactRepository contacts,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _ledger = ledger;
        _contacts = contacts;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DashboardDto> ExecuteAsync(DateOnly? asOf, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        // Due dates are calendar days. Without a client-supplied date we use today in UTC.
        var date = asOf ?? DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        var entries = await _ledger.ListByUserAsync(user.Id, cancellationToken);
        var contacts = await _contacts.ListAllAsync(user.Id, cancellationToken);
        var names = contacts.ToDictionary(c => c.Id, c => c.Name);

        var summary = DashboardCalculator.Calculate(entries, date, Currency.PHP);

        var recent = entries
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(RecentTransactionCount)
            .Select(e => new DashboardTransactionDto(
                e.Id,
                e.ContactId,
                NameOf(names, e.ContactId),
                e.Type,
                e.Amount.Amount,
                e.SignedAmount.Amount,
                e.OccurredAt,
                e.ReversesEntryId))
            .ToList();

        return new DashboardDto(
            date,
            Currency.PHP,
            summary.TotalOwedToMe.Amount,
            summary.TotalIOwe.Amount,
            summary.NetBalance.Amount,
            summary.ActiveContactCount,
            ToGroup(summary.Overdue, names),
            ToGroup(summary.Upcoming, names),
            recent);
    }

    private static DashboardDueGroupDto ToGroup(
        IReadOnlyList<OutstandingDebt> debts,
        IReadOnlyDictionary<Guid, string> names) =>
        new(
            debts.Count,
            debts.Where(d => d.Direction == DebtDirection.OwedToMe).Sum(d => d.Outstanding.Amount),
            debts.Where(d => d.Direction == DebtDirection.IOwe).Sum(d => d.Outstanding.Amount),
            debts.Take(MaxDueItems)
                .Select(d => new DashboardDebtItemDto(
                    d.Entry.Id,
                    d.Entry.ContactId,
                    NameOf(names, d.Entry.ContactId),
                    d.Direction,
                    d.Outstanding.Amount,
                    d.Entry.DueDate!.Value))
                .ToList());

    private static string NameOf(IReadOnlyDictionary<Guid, string> names, Guid contactId) =>
        names.TryGetValue(contactId, out var name) ? name : "Unknown contact";
}