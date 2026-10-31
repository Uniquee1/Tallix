import type { Repositories } from "./contracts";
import { localContactReads, localDashboard, localLedgerReads, localUsers } from "./local";
import { writeThroughContacts, writeThroughLedger } from "./writeThrough";

// The one place that decides where data comes from.
// Reads come from the phone's database. Writes still go to the server first (Part D changes that).
export const repositories: Repositories = {
  users: localUsers,
  dashboard: localDashboard,
  contacts: { ...localContactReads, ...writeThroughContacts },
  ledger: { ...localLedgerReads, ...writeThroughLedger },
};