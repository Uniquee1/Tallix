import type { Contact, Dashboard, PagedResult, User } from "../types";

export type ContactListParams = {
  search: string;
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
}

export type Repositories = {
  users: UserRepository;
  dashboard: DashboardRepository;
  contacts: ContactRepository;
};