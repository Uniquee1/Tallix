# Shared ledger test cases

`ledger-cases.json` is the specification of the ledger rules (balance, running
balance, outstanding debts, dashboard). It is data, so two implementations can
be held to the same behaviour:

- the C# domain: `tests/unit/Tallix.Domain.Tests/SharedLedgerCasesTests.cs`
- the mobile app (TypeScript): `apps/mobile/src/lib/ledger/sharedCases.test.ts`

The C# domain is the reference. A case is added or changed here first, proven
against the C# code, and then the TypeScript must agree.

## Format

- Money is in whole centavos (`500000` is ₱5,000.00).
- `at` is days after `2026-09-01T08:00:00Z`. `createdHoursLater` (optional)
  records the entry that many hours after it happened.
- Ids are small integers. Both harnesses expand `7` to
  `00000000-0000-0000-0000-000000000007` (contacts use a `c0000000` prefix).
- `dueDate` and `asOf` are calendar days (`yyyy-MM-dd`).
- An entry with `reversesEntryId` is a reversal: an ADJUSTMENT that cancels the
  entry it names. Its `at` is the moment of the reversal, and its amount must
  equal what the domain would create.
- `expectError: true` means the rule must refuse the input.