import type {
  Contact,
  ContactInput,
  ContactLedger,
  Dashboard,
  LedgerEntry,
  PagedResult,
  RecordEntryInput,
  User,
} from "../types";

export type ContactListParams = {
  search: string;
  page: number;
  pageSize: number;
};

export type LedgerPageParams = {
  contactId: string;
  page: number;
  pageSize: number;
};

export interface UserRepository {
  me(): Promise<User>;
}

export interface DashboardRepository {
  get(asOf: string): Promise<Dashboard>;
}

export interface ContactRepository {
  list(params: ContactListParams): Promise<PagedResult<Contact>>;
  get(id: string): Promise<Contact>;
  create(input: ContactInput): Promise<Contact>;
  update(id: string, input: ContactInput): Promise<Contact>;
  remove(id: string): Promise<void>;
}

export interface LedgerRepository {
  forContact(params: LedgerPageParams): Promise<ContactLedger>;
  record(contactId: string, input: RecordEntryInput): Promise<LedgerEntry>;
  reverse(entryId: string, description: string | null): Promise<LedgerEntry>;
}

export type Repositories = {
  users: UserRepository;
  dashboard: DashboardRepository;
  contacts: ContactRepository;
  ledger: LedgerRepository;
};