import rawCases from "../../../../../tests/shared/ledger-cases.json";
import type { TransactionType } from "../types";
import { reversalAmountCentavos } from "./entry";
import { normalizeContact, normalizeEntry } from "./validate";

type EntryCase = {
  name: string;
  type: TransactionType;
  amountCentavos: number;
  description?: string;
  descriptionLength?: number;
  expectedError: string | null;
  expectedDescription?: string;
};

type ContactFields = {
  name?: string;
  nameLength?: number;
  phone?: string;
  phoneLength?: number;
  email?: string;
  emailLength?: number;
  notes?: string;
  notesLength?: number;
};

type ContactCase = {
  name: string;
  input: ContactFields;
  expected?: { name: string; phone: string | null; email: string | null; notes: string | null };
  expectedError?: string;
};

type EntryJson = { id: number; type: TransactionType; amountCentavos: number; reversesEntryId?: number };
type LedgerCase = { entries: EntryJson[] };

const file = rawCases as unknown as {
  entryValidation: EntryCase[];
  contactNormalization: ContactCase[];
  balance: LedgerCase[];
  runningBalance: LedgerCase[];
  outstanding: LedgerCase[];
  dashboard: LedgerCase[];
};

// "...Length: 201" in the file means "a text of 201 characters".
const text = (value: string | undefined, length: number | undefined) =>
  length !== undefined ? "x".repeat(length) : value;

describe("shared validation cases", () => {
  it("gives every case its own name", () => {
    const entryNames = file.entryValidation.map((c) => c.name);
    const contactNames = file.contactNormalization.map((c) => c.name);

    expect(new Set(entryNames).size).toBe(entryNames.length);
    expect(new Set(contactNames).size).toBe(contactNames.length);
  });

  describe("entry validation", () => {
    it.each(file.entryValidation.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      const run = () =>
        normalizeEntry({
          type: testCase.type,
          amountCentavos: testCase.amountCentavos,
          description: text(testCase.description, testCase.descriptionLength) ?? null,
        });

      if (testCase.expectedError !== null) {
        expect(run).toThrow(new Error(testCase.expectedError));
        return;
      }

      const result = run();

      if (testCase.expectedDescription !== undefined) {
        expect(result.description).toBe(testCase.expectedDescription);
      }
    });
  });

  describe("contact normalization", () => {
    it.each(file.contactNormalization.map((c) => [c.name, c] as const))("%s", (_name, testCase) => {
      const input = testCase.input;
      const run = () =>
        normalizeContact({
          name: text(input.name, input.nameLength) ?? "",
          phone: text(input.phone, input.phoneLength) ?? null,
          email: text(input.email, input.emailLength) ?? null,
          notes: text(input.notes, input.notesLength) ?? null,
        });

      if (testCase.expectedError !== undefined) {
        expect(run).toThrow(new Error(testCase.expectedError));
        return;
      }

      const result = run();

      if (testCase.expected) {
        expect(result).toEqual(testCase.expected);
      }
    });
  });

  it("builds every reversal in the file the way the file describes it", () => {
    const cases = [...file.balance, ...file.runningBalance, ...file.outstanding, ...file.dashboard];
    let reversals = 0;

    for (const testCase of cases) {
      for (const entry of testCase.entries) {
        if (entry.reversesEntryId === undefined) {
          continue;
        }

        const original = testCase.entries.find((e) => e.id === entry.reversesEntryId);

        if (!original) {
          throw new Error(`Reversal ${entry.id} names an entry that is not in its case.`);
        }

        expect(reversalAmountCentavos(original)).toBe(entry.amountCentavos);
        reversals += 1;
      }
    }

    expect(reversals).toBeGreaterThan(0);
  });
});