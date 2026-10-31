using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

// Runs tests/shared/ledger-cases.json. The mobile app's TypeScript tests run the SAME
// file, so the two implementations of the ledger rules cannot quietly drift apart.
public class SharedLedgerCasesTests
{
    private static readonly DateTimeOffset Base = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-00000000ffff");

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        // The same text form the API uses on the wire: LENT, PAYMENT_RECEIVED, I_OWE...
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private static readonly Lazy<CaseFile> Cases = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "shared", "ledger-cases.json");

        return JsonSerializer.Deserialize<CaseFile>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("ledger-cases.json is empty.");
    });

    // ---------- The file's shape ----------

    private sealed record CaseFile(
        List<BalanceCase> Balance,
        List<RunningCase> RunningBalance,
        List<OutstandingCase> Outstanding,
        List<DashboardCase> Dashboard);

    private sealed record EntryJson(
        int Id,
        int ContactId,
        TransactionType Type,
        long AmountCentavos,
        int At,
        int? CreatedHoursLater,
        string? DueDate,
        int? ReversesEntryId);

    private sealed record BalanceCase(string Name, List<EntryJson> Entries, long ExpectedCentavos);

    private sealed record RunningExpected(int Id, long BalanceCentavos);

    private sealed record RunningCase(string Name, List<EntryJson> Entries, List<RunningExpected> Expected);

    private sealed record DebtExpected(int Id, long OutstandingCentavos, DebtDirection Direction);

    private sealed record OutstandingCase(
        string Name, List<EntryJson> Entries, List<DebtExpected>? Expected, bool ExpectError);

    private sealed record DashboardExpected(
        long TotalOwedToMeCentavos,
        long TotalIOweCentavos,
        int ActiveContactCount,
        List<DebtExpected> Overdue,
        List<DebtExpected> Upcoming);

    private sealed record DashboardCase(
        string Name, string AsOf, List<EntryJson> Entries, DashboardExpected Expected);

    // ---------- How the file's values become real ones (the TypeScript side does the same) ----------

    private static Guid IdOf(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static Guid ContactIdOf(int n) => new($"c0000000-0000-0000-0000-{n:D12}");

    private static int NumberOf(Guid id) =>
        int.Parse(id.ToString()[^12..], CultureInfo.InvariantCulture);

    private static long Centavos(Money money) => (long)(money.Amount * 100m);

    private static LedgerEntry[] Build(IEnumerable<EntryJson> rows)
    {
        var byId = new Dictionary<int, LedgerEntry>();
        var result = new List<LedgerEntry>();

        foreach (var row in rows)
        {
            var occurredAt = Base.AddDays(row.At);
            LedgerEntry entry;

            if (row.ReversesEntryId is { } originalId)
            {
                entry = LedgerEntry.CreateReversal(IdOf(row.Id), byId[originalId], occurredAt);

                // The file must describe a reversal exactly as the domain builds one.
                Assert.Equal(row.AmountCentavos, Centavos(entry.Amount));
                Assert.Equal(ContactIdOf(row.ContactId), entry.ContactId);
            }
            else
            {
                entry = LedgerEntry.Create(
                    IdOf(row.Id),
                    UserId,
                    ContactIdOf(row.ContactId),
                    row.Type,
                    Money.Php(row.AmountCentavos / 100m),
                    occurredAt,
                    occurredAt.AddHours(row.CreatedHoursLater ?? 0),
                    dueDate: row.DueDate is null
                        ? null
                        : DateOnly.ParseExact(row.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            byId[row.Id] = entry;
            result.Add(entry);
        }

        return result.ToArray();
    }

    private static List<(int, long, DebtDirection)> Describe(IEnumerable<OutstandingDebt> debts) =>
        debts.Select(d => (NumberOf(d.Entry.Id), Centavos(d.Outstanding), d.Direction)).ToList();

    private static List<(int, long, DebtDirection)> Describe(IEnumerable<DebtExpected> expected) =>
        expected.Select(e => (e.Id, e.OutstandingCentavos, e.Direction)).ToList();

    // ---------- The tests ----------

    public static IEnumerable<object[]> BalanceNames =>
        Cases.Value.Balance.Select(c => new object[] { c.Name });

    public static IEnumerable<object[]> RunningNames =>
        Cases.Value.RunningBalance.Select(c => new object[] { c.Name });

    public static IEnumerable<object[]> OutstandingNames =>
        Cases.Value.Outstanding.Select(c => new object[] { c.Name });

    public static IEnumerable<object[]> DashboardNames =>
        Cases.Value.Dashboard.Select(c => new object[] { c.Name });

    [Fact]
    public void EveryCaseInASectionHasItsOwnName()
    {
        foreach (var names in new[]
                 {
                     Cases.Value.Balance.Select(c => c.Name).ToList(),
                     Cases.Value.RunningBalance.Select(c => c.Name).ToList(),
                     Cases.Value.Outstanding.Select(c => c.Name).ToList(),
                     Cases.Value.Dashboard.Select(c => c.Name).ToList()
                 })
        {
            Assert.Equal(names.Count, names.Distinct().Count());
        }
    }

    [Theory]
    [MemberData(nameof(BalanceNames))]
    public void Balance(string name)
    {
        var testCase = Cases.Value.Balance.Single(c => c.Name == name);

        var balance = BalanceCalculator.Calculate(Build(testCase.Entries), Currency.PHP);

        Assert.Equal(testCase.ExpectedCentavos, Centavos(balance));
    }

    [Theory]
    [MemberData(nameof(RunningNames))]
    public void RunningBalance(string name)
    {
        var testCase = Cases.Value.RunningBalance.Single(c => c.Name == name);

        var actual = BalanceCalculator.WithRunningBalance(Build(testCase.Entries), Currency.PHP)
            .Select(r => (NumberOf(r.Entry.Id), Centavos(r.Balance)))
            .ToList();

        Assert.Equal(testCase.Expected.Select(e => (e.Id, e.BalanceCentavos)).ToList(), actual);
    }

    [Theory]
    [MemberData(nameof(OutstandingNames))]
    public void Outstanding(string name)
    {
        var testCase = Cases.Value.Outstanding.Single(c => c.Name == name);
        var entries = Build(testCase.Entries);

        if (testCase.ExpectError)
        {
            Assert.Throws<DomainException>(() => OutstandingDebtCalculator.Calculate(entries, Currency.PHP));
            return;
        }

        Assert.Equal(
            Describe(testCase.Expected!),
            Describe(OutstandingDebtCalculator.Calculate(entries, Currency.PHP)));
    }

    [Theory]
    [MemberData(nameof(DashboardNames))]
    public void Dashboard(string name)
    {
        var testCase = Cases.Value.Dashboard.Single(c => c.Name == name);
        var asOf = DateOnly.ParseExact(testCase.AsOf, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var summary = DashboardCalculator.Calculate(Build(testCase.Entries), asOf, Currency.PHP);
        var expected = testCase.Expected;

        Assert.Equal(expected.TotalOwedToMeCentavos, Centavos(summary.TotalOwedToMe));
        Assert.Equal(expected.TotalIOweCentavos, Centavos(summary.TotalIOwe));
        Assert.Equal(expected.ActiveContactCount, summary.ActiveContactCount);
        Assert.Equal(Describe(expected.Overdue), Describe(summary.Overdue));
        Assert.Equal(Describe(expected.Upcoming), Describe(summary.Upcoming));
    }
}