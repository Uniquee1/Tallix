using System.Text.Json;
using System.Text.Json.Serialization;
using Tallix.Domain.Common;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

// Runs the validation sections of tests/shared/ledger-cases.json. The mobile app's
// TypeScript tests run the SAME sections, so the phone rejects exactly what the server rejects.
public class SharedValidationCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-00000000ffff");
    private static readonly Guid EntryId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ContactId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private static readonly Lazy<ValidationFile> Cases = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "shared", "ledger-cases.json");

        return JsonSerializer.Deserialize<ValidationFile>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("ledger-cases.json is empty.");
    });

    private sealed record ValidationFile(
        List<EntryCase> EntryValidation,
        List<ContactCase> ContactNormalization);

    private sealed record EntryCase(
        string Name,
        TransactionType Type,
        long AmountCentavos,
        string? Description,
        int? DescriptionLength,
        string? ExpectedError,
        string? ExpectedDescription);

    private sealed record ContactFields(
        string? Name, int? NameLength,
        string? Phone, int? PhoneLength,
        string? Email, int? EmailLength,
        string? Notes, int? NotesLength);

    private sealed record ContactExpected(string Name, string? Phone, string? Email, string? Notes);

    private sealed record ContactCase(
        string Name, ContactFields Input, ContactExpected? Expected, string? ExpectedError);

    // "...Length: 201" in the file means "a text of 201 characters".
    private static string? Text(string? value, int? length) =>
        length is { } n ? new string('x', n) : value;

    public static IEnumerable<object[]> EntryNames =>
        Cases.Value.EntryValidation.Select(c => new object[] { c.Name });

    public static IEnumerable<object[]> ContactNames =>
        Cases.Value.ContactNormalization.Select(c => new object[] { c.Name });

    [Fact]
    public void EveryCaseHasItsOwnName()
    {
        var entryNames = Cases.Value.EntryValidation.Select(c => c.Name).ToList();
        var contactNames = Cases.Value.ContactNormalization.Select(c => c.Name).ToList();

        Assert.Equal(entryNames.Count, entryNames.Distinct().Count());
        Assert.Equal(contactNames.Count, contactNames.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(EntryNames))]
    public void EntryValidation(string name)
    {
        var testCase = Cases.Value.EntryValidation.Single(c => c.Name == name);
        var description = Text(testCase.Description, testCase.DescriptionLength);

        LedgerEntry Create() => LedgerEntry.Create(
            EntryId, UserId, ContactId, testCase.Type,
            Money.Php(testCase.AmountCentavos / 100m), Now, Now, description);

        if (testCase.ExpectedError is { } message)
        {
            var error = Assert.Throws<DomainException>(Create);
            Assert.Equal(message, error.Message);
            return;
        }

        var entry = Create();

        if (testCase.ExpectedDescription is not null)
            Assert.Equal(testCase.ExpectedDescription, entry.Description);
    }

    [Theory]
    [MemberData(nameof(ContactNames))]
    public void ContactNormalization(string name)
    {
        var testCase = Cases.Value.ContactNormalization.Single(c => c.Name == name);
        var input = testCase.Input;

        Contact Create() => Contact.Create(
            ContactId, UserId, Text(input.Name, input.NameLength) ?? "", Now,
            Text(input.Phone, input.PhoneLength),
            Text(input.Email, input.EmailLength),
            Text(input.Notes, input.NotesLength));

        if (testCase.ExpectedError is { } message)
        {
            var error = Assert.Throws<DomainException>(Create);
            Assert.Equal(message, error.Message);
            return;
        }

        var contact = Create();

        if (testCase.Expected is { } expected)
        {
            Assert.Equal(expected.Name, contact.Name);
            Assert.Equal(expected.Phone, contact.Phone);
            Assert.Equal(expected.Email, contact.Email);
            Assert.Equal(expected.Notes, contact.Notes);
        }
    }
}