using Tallix.Application.Common;
using Tallix.Application.Users;

namespace Tallix.Application.Ledger;

public sealed class GetLedgerEntry
{
    private readonly ILedgerRepository _ledger;
    private readonly ICurrentUser _currentUser;

    public GetLedgerEntry(ILedgerRepository ledger, ICurrentUser currentUser)
    {
        _ledger = ledger;
        _currentUser = currentUser;
    }

    public async Task<LedgerEntryDto> ExecuteAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetAsync(cancellationToken);

        // The same answer whether the entry doesn't exist or belongs to someone else.
        var entry = await _ledger.GetByIdAsync(user.Id, entryId, cancellationToken)
            ?? throw new NotFoundException("Ledger entry not found.");

        return LedgerEntryDto.From(entry);
    }
}