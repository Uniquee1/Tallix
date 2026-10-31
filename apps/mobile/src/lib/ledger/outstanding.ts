import type { DebtDirection } from "../types";
import { balanceCentavos } from "./balance";
import { compareByTime, compareText, type LedgerRow } from "./entry";

export type OutstandingDebt = {
  entry: LedgerRow;
  outstandingCentavos: number;
  direction: DebtDirection;
};

// Dated debts first, oldest due date first, then the same order as everything else.
function compareDebts(a: LedgerRow, b: LedgerRow): number {
  if ((a.dueDate === null) !== (b.dueDate === null)) {
    return a.dueDate === null ? 1 : -1;
  }

  if (a.dueDate !== null && b.dueDate !== null && a.dueDate !== b.dueDate) {
    return compareText(a.dueDate, b.dueDate);
  }

  return compareByTime(a, b);
}

// OutstandingDebtCalculator.Calculate: what is still outstanding for ONE contact.
// Payments are applied to dated debts first (oldest due date first), then to undated ones.
export function outstandingDebts(entries: readonly LedgerRow[]): OutstandingDebt[] {
  if (new Set(entries.map((e) => e.contactId)).size > 1) {
    throw new Error("Outstanding debts are worked out for one contact at a time.");
  }

  const balance = balanceCentavos(entries);

  if (balance === 0) {
    return [];
  }

  const direction: DebtDirection = balance > 0 ? "OWED_TO_ME" : "I_OWE";
  const debtType = balance > 0 ? "LENT" : "BORROWED";

  // A reversed entry is cancelled by its reversal, so it is no longer a debt.
  const reversedIds = new Set(
    entries.filter((e) => e.reversesEntryId !== null).map((e) => e.reversesEntryId),
  );

  const debts = entries
    .filter((e) => e.type === debtType && !reversedIds.has(e.id))
    .sort(compareDebts);

  // Whatever the balance doesn't account for has been settled by payments and the like.
  const totalDebt = debts.reduce((sum, debt) => sum + debt.amountCentavos, 0);
  let settled = Math.max(totalDebt - Math.abs(balance), 0);

  const result: OutstandingDebt[] = [];

  for (const debt of debts) {
    const absorbed = Math.min(debt.amountCentavos, settled);
    settled -= absorbed;

    const outstanding = debt.amountCentavos - absorbed;

    if (outstanding > 0) {
      result.push({ entry: debt, outstandingCentavos: outstanding, direction });
    }
  }

  return result;
}