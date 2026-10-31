import { compareByTime, signedCentavos, type LedgerRow } from "./entry";

// BalanceCalculator.Calculate: positive means the contact owes the user.
export function balanceCentavos(entries: readonly LedgerRow[]): number {
  let balance = 0;

  for (const entry of entries) {
    balance += signedCentavos(entry);
  }

  return balance;
}

export type RunningBalance = { entry: LedgerRow; balanceCentavos: number };

// BalanceCalculator.WithRunningBalance: each entry with the balance right after it, oldest first.
export function withRunningBalance(entries: readonly LedgerRow[]): RunningBalance[] {
  let balance = 0;

  return [...entries].sort(compareByTime).map((entry) => {
    balance += signedCentavos(entry);

    return { entry, balanceCentavos: balance };
  });
}