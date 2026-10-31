import { balanceCentavos } from "./balance";
import { addDays } from "./calendar";
import { compareText, type LedgerRow } from "./entry";
import { outstandingDebts, type OutstandingDebt } from "./outstanding";

export const UPCOMING_DAYS = 7;

export type DashboardSummary = {
  totalOwedToMeCentavos: number;
  totalIOweCentavos: number;
  activeContactCount: number;
  overdue: OutstandingDebt[];
  upcoming: OutstandingDebt[];
};

const dueOf = (debt: OutstandingDebt) => debt.entry.dueDate ?? "";

function byDueDate(a: OutstandingDebt, b: OutstandingDebt): number {
  return compareText(dueOf(a), dueOf(b)) || compareText(a.entry.id, b.entry.id);
}

// DashboardCalculator.Calculate: works out the dashboard figures from ALL of a user's entries.
// Balances are netted within a contact, but never across contacts.
export function dashboardSummary(entries: readonly LedgerRow[], asOf: string): DashboardSummary {
  const byContact = new Map<string, LedgerRow[]>();

  for (const entry of entries) {
    const list = byContact.get(entry.contactId);

    if (list) {
      list.push(entry);
    } else {
      byContact.set(entry.contactId, [entry]);
    }
  }

  const horizon = addDays(asOf, UPCOMING_DAYS);
  let owedToMe = 0;
  let iOwe = 0;
  let active = 0;
  const overdue: OutstandingDebt[] = [];
  const upcoming: OutstandingDebt[] = [];

  for (const contactEntries of byContact.values()) {
    const balance = balanceCentavos(contactEntries);

    // A zero balance means nothing is outstanding, so the contact isn't active.
    if (balance === 0) {
      continue;
    }

    active += 1;

    if (balance > 0) {
      owedToMe += balance;
    } else {
      iOwe += -balance;
    }

    for (const debt of outstandingDebts(contactEntries)) {
      const due = debt.entry.dueDate;

      if (due === null) {
        continue;
      }

      if (due < asOf) {
        overdue.push(debt);
      } else if (due <= horizon) {
        upcoming.push(debt);
      }
    }
  }

  return {
    totalOwedToMeCentavos: owedToMe,
    totalIOweCentavos: iOwe,
    activeContactCount: active,
    overdue: overdue.sort(byDueDate),
    upcoming: upcoming.sort(byDueDate),
  };
}