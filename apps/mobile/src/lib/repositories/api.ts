import { apiGet } from "../api";
import type { Contact, Dashboard, PagedResult, User } from "../types";
import type { Repositories } from "./contracts";

export const apiRepositories: Repositories = {
  users: {
    me: () => apiGet<User>("/me"),
  },

  dashboard: {
    get: (asOf) => apiGet<Dashboard>(`/dashboard?asOf=${encodeURIComponent(asOf)}`),
  },

  contacts: {
    list: ({ search, page, pageSize }) =>
      apiGet<PagedResult<Contact>>(
        `/contacts?search=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}`,
      ),
  },
};