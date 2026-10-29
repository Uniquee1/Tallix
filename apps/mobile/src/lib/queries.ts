import {
  keepPreviousData,
  skipToken,
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import { localToday } from "./format";
import { repositories } from "./repositories";
import type { ContactInput, RecordEntryInput } from "./types";

export const CONTACTS_PAGE_SIZE = 20;
export const HISTORY_PAGE_SIZE = 20;

// ---------- Reads ----------

export function useMe() {
  return useQuery({ queryKey: ["me"], queryFn: () => repositories.users.me() });
}

export function useDashboard() {
  return useQuery({
    queryKey: ["dashboard"],
    // The date is read here, when the request is made, never while rendering.
    queryFn: () => repositories.dashboard.get(localToday()),
  });
}

export function useContactsList(search: string) {
  return useInfiniteQuery({
    queryKey: ["contacts", { search }],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      repositories.contacts.list({ search, page: pageParam, pageSize: CONTACTS_PAGE_SIZE }),
    getNextPageParam: (last) =>
      last.page * last.pageSize < last.totalCount ? last.page + 1 : undefined,
    // Keep showing the previous results while a new search loads.
    placeholderData: keepPreviousData,
  });
}

// With no id (the "new contact" form) the query is switched off.
export function useContact(id: string | undefined) {
  return useQuery({
    queryKey: ["contact", id],
    queryFn: id ? () => repositories.contacts.get(id) : skipToken,
  });
}

export function useContactLedger(contactId: string) {
  return useInfiniteQuery({
    queryKey: ["ledger", contactId],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      repositories.ledger.forContact({ contactId, page: pageParam, pageSize: HISTORY_PAGE_SIZE }),
    getNextPageParam: (last) =>
      last.history.page * last.history.pageSize < last.history.totalCount
        ? last.history.page + 1
        : undefined,
  });
}

// ---------- Writes ----------

export function useCreateContact() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ContactInput) => repositories.contacts.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

export function useUpdateContact() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ContactInput }) =>
      repositories.contacts.update(id, input),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["contacts"] }),
        queryClient.invalidateQueries({ queryKey: ["contact"] }),
        // The dashboard shows contact names, so it is out of date too.
        queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      ]),
  });
}

export function useDeleteContact() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => repositories.contacts.remove(id),
    onSuccess: (_result, id) => {
      // The contact is gone, so nothing may try to load it again.
      queryClient.removeQueries({ queryKey: ["contact", id] });
      queryClient.removeQueries({ queryKey: ["ledger", id] });
      return queryClient.invalidateQueries({ queryKey: ["contacts"] });
    },
  });
}

export function useRecordEntry(contactId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: RecordEntryInput) => repositories.ledger.record(contactId, input),
    onSuccess: () => invalidateLedgerData(queryClient, contactId),
  });
}

export function useReverseEntry(contactId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ entryId, description }: { entryId: string; description: string | null }) =>
      repositories.ledger.reverse(entryId, description),
    onSuccess: () => invalidateLedgerData(queryClient, contactId),
  });
}

function invalidateLedgerData(queryClient: QueryClient, contactId: string) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ["ledger", contactId] }),
    // Totals, due dates and recent transactions all come from the same entries.
    queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
  ]);
}