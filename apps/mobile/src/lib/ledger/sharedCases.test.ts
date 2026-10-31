import rawCases from "../../../../../tests/shared/ledger-cases.json";
import type { DebtDirection, TransactionType } from "../types";
import { balanceCentavos, withRunningBalance } from "./balance";
import { dashboardSummary } from "./dashboard";
import type { LedgerRow } from "./entry";
import { outstandingDebts } from "./outstanding";

// ---------- The file's shape ----------

type EntryJson = {
  id: number;
  contactId: number;
  type: TransactionType;
  amountCentavos: number;
  at: number;
  createdHoursLater?: number;
  dueDate?: string;
  reversesEntryId?: number;
};

type DebtExpected = { id: number; outstandingCentavos: number; direction: DebtDirection };

type CaseFile = {
  balance: { name: string; entries: EntryJson[]; expectedCentavos: number }[];
  runningBalance: {
    name: string;
    entries: EntryJson[];
    expected: { id: number; balanceCentavos: number }[];
  }[];
  outstanding: {
    name: string;
    entries: EntryJson[];
    expected?: DebtExpected[];
    expectError?: boolean;
  }[];
  dashboard: {
    name: string;
    asOf: string;
    entries: EntryJson[];
    expected: {
      totalOwedToMeCentavos: number;
      totalIOweCentavos: number;
      activeContactCount: number;
      overdue: DebtExpected[];
      upcoming: DebtExpected[];
    };
  }[];
};

const file = rawCases as unknown as CaseFile;

// ---------- How the file's values become real ones (the C# side does the same) ----------

const BASE_MS = Date.UTC(2026, 8, 1, 8, 0, 0); // 2026-09-01T08:00:00Z
const DAY_MS = 24 * 60 * 60 * 1000;
const HOUR_MS = 60 * 60 * 1000;

const entryId = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, "0")}`;
const contactId = (n: number) => `c0000000-0000-0000-0000-${String(n).padStart(12, "0")}`;
const numberOf = (id: string) => Number(id.slice(-12));

function toRows(entries: EntryJson[]): LedgerRow[] {
  return entries.map((e) => {
    const occurred = BASE_MS + e.at * DAY_MS;
    const created = occurred + (e.createdHoursLater ?? 0) * HOUR_MS;

    return {
      id: entryId(e.id),
      contactId: contactId(e.contactId),
      type: e.type,
      amountCentavos: e.amountCentavos,
      occurredAt: new Date(occurred).toISOString(),
      createdAt: new Date(created).toISOString(),
      dueDate: e.dueDate ?? null,
      reversesEntryId: e.reversesEntryId === undefined ? null : entryId(e.reversesEntryId),
    };
  });
}

const describeDebts = (debts: { entry: LedgerRow; outstandingCentavos: number; direction: DebtDirection }[]) =>
  debts.map((d) => ({
    id: numberOf(d.entry.id),
    outstandingCentavos: d.outstandingCentavos,
    direction: d.direction,
  }));

// ---------- The tests ----------

describe("shared ledger cases", () => {
  it("gives every case in a section its own name", () => {
    for (const section of [file.balance, file.runningBalance, file.outstanding, file.dashboard]) {
      const names = section.map((c) => c.name);

      expect(new Set(names).size).toBe(names.length);
    }
  });

  describe("balance", () => {
    it.each(file.balance.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      expect(balanceCentavos(toRows(testCase.entries))).toBe(testCase.expectedCentavos);
    });
  });

  describe("running balance", () => {
    it.each(file.runningBalance.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      const actual = withRunningBalance(toRows(testCase.entries)).map((r) => ({
        id: numberOf(r.entry.id),
        balanceCentavos: r.balanceCentavos,
      }));

      expect(actual).toEqual(testCase.expected);
    });
  });

  describe("outstanding debts", () => {
    it.each(file.outstanding.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      const rows = toRows(testCase.entries);

      if (testCase.expectError) {
        expect(() => outstandingDebts(rows)).toThrow();
        return;
      }

      expect(describeDebts(outstandingDebts(rows))).toEqual(testCase.expected);
    });
  });

  describe("dashboard", () => {
    it.each(file.dashboard.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      const summary = dashboardSummary(toRows(testCase.entries), testCase.asOf);
      const expected = testCase.expected;

      expect(summary.totalOwedToMeCentavos).toBe(expected.totalOwedToMeCentavos);
      expect(summary.totalIOweCentavos).toBe(expected.totalIOweCentavos);
      expect(summary.activeContactCount).toBe(expected.activeContactCount);
      expect(describeDebts(summary.overdue)).toEqual(expected.overdue);
      expect(describeDebts(summary.upcoming)).toEqual(expected.upcoming);
    });
  });
});