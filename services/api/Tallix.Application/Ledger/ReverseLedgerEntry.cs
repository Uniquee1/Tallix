using Tallix.Application.Common;
using Tallix.Application.Users;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Ledger;

public sealed record ReverseLedgerEntryRequest(string? Description);

public sealed class ReverseLedgerEntry
{
    private readonly ILedgerRepository _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public ReverseLedgerEntry(
        ILedgerRepository ledger,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<LedgerEntryDto> ExecuteAsync(
        Guid entryId,
        string? description,
        CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        // Someone else's entry looks exactly like a missing one.
        var original = await _ledger.GetByIdAsync(user.Id, entryId, cancellationToken)
            ?? throw new NotFoundException("Ledger entry not found.");

        // Throws a DomainException (400) if the original is itself a reversal.
        var reversal = LedgerEntry.CreateReversal(
            Guid.NewGuid(), original, _clock.GetUtcNow(), description);

        // The unique index decides whether this entry was already reversed. Checking
        // first and then inserting would leave a gap for two requests to slip through.
        if (!await _ledger.TryAddReversalAsync(reversal, cancellationToken))
            throw new ConflictException("This entry has already been reversed.");

        return LedgerEntryDto.From(reversal);
    }
}