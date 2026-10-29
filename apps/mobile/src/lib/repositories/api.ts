import { apiDelete, apiGet, apiPost, apiPut } from "../api";
import type { Contact, ContactLedger, Dashboard, LedgerEntry, PagedResult, User } from "../types";
import type { Repositories } from "./contracts";

const enc = encodeURIComponent;

export const apiRepositories: Repositories = {
  users: {
    me: () => apiGet<User>("/me"),
  },

  dashboard: {
    get: (asOf) => apiGet<Dashboard>(`/dashboard?asOf=${enc(asOf)}`),
  },

  contacts: {
    list: ({ search, page, pageSize }) =>
      apiGet<PagedResult<Contact>>(
        `/contacts?search=${enc(search)}&page=${page}&pageSize=${pageSize}`,
      ),
    get: (id) => apiGet<Contact>(`/contacts/${enc(id)}`),
    create: (input) => apiPost<Contact>("/contacts", input),
    update: (id, input) => apiPut<Contact>(`/contacts/${enc(id)}`, input),
    remove: (id) => apiDelete(`/contacts/${enc(id)}`),
  },

  ledger: {
    forContact: ({ contactId, page, pageSize }) =>
      apiGet<ContactLedger>(
        `/contacts/${enc(contactId)}/ledger?page=${page}&pageSize=${pageSize}`,
      ),
    record: (contactId, input) =>
      apiPost<LedgerEntry>(`/contacts/${enc(contactId)}/ledger`, input),
    reverse: (entryId, description) =>
      apiPost<LedgerEntry>(`/ledger/${enc(entryId)}/reverse`, { description }),
  },
};