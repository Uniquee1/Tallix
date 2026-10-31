import type { TransactionType } from "../types";

// One ledger entry, with just the fields the rules need. Money is in whole centavos.
export type LedgerRow = {
  id: string;
  contactId: string;
  type: TransactionType;
  amountCentavos: number;
  occurredAt: string; // an instant, as UTC text
  createdAt: string;
  dueDate: string | null; // a calendar day, yyyy-MM-dd
  reversesEntryId: string | null;
};

// How an entry moves the balance. Positive means the contact owes the user more.
// Same table as LedgerEntry.SignedAmount on the server.
export function signedCentavos(entry: Pick<LedgerRow, "type" | "amountCentavos">): number {
  switch (entry.type) {
    case "LENT":
    case "PAYMENT_MADE":
    case "ADJUSTMENT":
      return entry.amountCentavos;
    case "BORROWED":
    case "PAYMENT_RECEIVED":
      return -entry.amountCentavos;
    default: {
      const unknown: never = entry.type;
      throw new Error(`Unknown transaction type: ${String(unknown)}`);
    }
  }
}

// Plain text comparison. Ids are lowercase GUID text, so this matches the server's id order.
export function compareText(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0;
}

// The server's order: when it happened, then when it was recorded, then id.
// Instants are compared as numbers, never as text.
export function compareByTime(a: LedgerRow, b: LedgerRow): number {
  return (
    Date.parse(a.occurredAt) - Date.parse(b.occurredAt) ||
    Date.parse(a.createdAt) - Date.parse(b.createdAt) ||
    compareText(a.id, b.id)
  );
}