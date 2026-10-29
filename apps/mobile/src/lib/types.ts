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

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

// Matches ContactDto in the API.
export type Contact = {
  id: string;
  name: string;
  phone: string | null;
  email: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
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
// All arithmetic happens on the server (for now: see Milestone 9).
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

// What we send to create or replace a contact.
export type ContactInput = {
  name: string;
  phone: string | null;
  email: string | null;
  notes: string | null;
};

// Matches LedgerEntryDto in the API.
export type LedgerEntry = {
  id: string;
  contactId: string;
  type: TransactionType;
  amount: number;
  currency: "PHP";
  signedAmount: number;
  description: string | null;
  occurredAt: string;
  dueDate: string | null;
  createdAt: string;
  reversesEntryId: string | null;
};

// Matches LedgerHistoryItemDto: one row of a contact's history.
export type LedgerHistoryItem = {
  id: string;
  type: TransactionType;
  amount: number;
  currency: "PHP";
  signedAmount: number;
  runningBalance: number;
  description: string | null;
  occurredAt: string;
  dueDate: string | null;
  createdAt: string;
  reversesEntryId: string | null;
  reversedByEntryId: string | null;
};

export type ContactLedger = {
  contactId: string;
  balance: number;
  currency: "PHP";
  history: PagedResult<LedgerHistoryItem>;
};

// What we send to record a transaction. A null occurredAt means "now" on the server.
export type RecordEntryInput = {
  type: TransactionType;
  amount: number;
  occurredAt: string | null;
  dueDate: string | null;
  description: string | null;
};