// Matches UserDto in the API.
export type User = {
  id: string;
  email: string;
  displayName: string;
};

// What the API returns for errors (problem details).
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
};

// The API sends these as text, in these exact forms.
export type TransactionType =
  | "LENT"
  | "BORROWED"
  | "PAYMENT_RECEIVED"
  | "PAYMENT_MADE"
  | "ADJUSTMENT";

export type DebtDirection = "OWED_TO_ME" | "I_OWE";

// Money arrives as plain numbers and is only ever displayed here.
// All arithmetic happens on the server.
export type DashboardDebtItem = {
  entryId: string;
  contactId: string;
  contactName: string;
  direction: DebtDirection;
  outstanding: number;
  dueDate: string; // a calendar date, like "2026-11-15"
};

export type DashboardDueGroup = {
  count: number;
  owedToMeTotal: number;
  iOweTotal: number;
  items: DashboardDebtItem[];
};

export type DashboardTransaction = {
  entryId: string;
  contactId: string;
  contactName: string;
  type: TransactionType;
  amount: number;
  signedAmount: number;
  occurredAt: string; // an instant, like "2026-10-23T06:30:00+00:00"
  reversesEntryId: string | null;
};

export type Dashboard = {
  asOf: string;
  currency: "PHP";
  totalOwedToMe: number;
  totalIOwe: number;
  netBalance: number;
  activeContactCount: number;
  overdue: DashboardDueGroup;
  upcoming: DashboardDueGroup;
  recentTransactions: DashboardTransaction[];
};