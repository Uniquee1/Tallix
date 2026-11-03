using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Application.Tests;

public class RecordLedgerEntryClockTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TheWebFormIsHeldToTheSameClockRule()
    {
        var contacts = new InMemoryContactRepository();
        var ledger = new InMemoryLedgerRepository();
        var user = new FakeCurrentUser(FakeCurrentUser.NewUser("Alice"));
        var contact = Contact.Create(Guid.NewGuid(), user.User.Id, "Juan", Now);
        contacts.Seed(contact);

        var record = new RecordLedgerEntry(ledger, contacts, user, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<DomainException>(() => record.ExecuteAsync(
            contact.Id,
            new RecordLedgerEntryRequest(TransactionType.Lent, 100m, null, Now.AddDays(3), null, null),
            CancellationToken.None));

        Assert.Empty(ledger.Entries);
    }
}