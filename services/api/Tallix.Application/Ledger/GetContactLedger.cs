using Tallix.Application.Common;
using Tallix.Application.Contacts;
using Tallix.Application.Users;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Ledger;

public sealed record LedgerHistoryItemDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    decimal SignedAmount,
    decimal RunningBalance,
    string? Description,
    DateTimeOffset OccurredAt,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    Guid? ReversesEntryId,
    Guid? ReversedByEntryId);

public sealed record ContactLedgerDto(
    Guid ContactId,
    decimal Balance,
    Currency Currency,
    PagedResult<LedgerHistoryItemDto> History);

public sealed class GetContactLedger
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly ILedgerRepository _ledger;
    private readonly IContactRepository _contacts;
    private readonly ICurrentUser _currentUser;

    public GetContactLedger(
        ILedgerRepository ledger,
        IContactRepository contacts,
        ICurrentUser currentUser)
    {
        _ledger = ledger;
        _contacts = contacts;
        _currentUser = currentUser;
    }

    public async Task<ContactLedgerDto> ExecuteAsync(
        Guid contactId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        var contact = await _contacts.GetByIdAsync(user.Id, contactId, cancellationToken)
            ?? throw new NotFoundException("Contact not found.");

        var entries = await _ledger.ListByContactAsync(user.Id, contact.Id, cancellationToken);

        var running = BalanceCalculator.WithRunningBalance(entries, Currency.PHP);

        // Which entry reversed which, so the original can point at its reversal.
        var reversedBy = entries
            .Where(e => e.ReversesEntryId is not null)
            .ToDictionary(e => e.ReversesEntryId!.Value, e => e.Id);

        var pageNumber = Math.Max(page, 1);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var skip = (int)Math.Min((long)(pageNumber - 1) * size, int.MaxValue);

        var items = Enumerable.Reverse(running)   // newest first
            .Skip(skip)
            .Take(size)
            .Select(r => new LedgerHistoryItemDto(
                r.Entry.Id,
                r.Entry.Type,
                r.Entry.Amount.Amount,
                r.Entry.Amount.Currency,
                r.Entry.SignedAmount.Amount,
                r.Balance.Amount,
                r.Entry.Description,
                r.Entry.OccurredAt,
                r.Entry.DueDate,
                r.Entry.CreatedAt,
                r.Entry.ReversesEntryId,
                reversedBy.TryGetValue(r.Entry.Id, out var reversalId) ? reversalId : null))
            .ToList();

        return new ContactLedgerDto(
            contact.Id,
            BalanceCalculator.Calculate(entries, Currency.PHP).Amount,
            Currency.PHP,
            new PagedResult<LedgerHistoryItemDto>(items, pageNumber, size, running.Count));
    }
}