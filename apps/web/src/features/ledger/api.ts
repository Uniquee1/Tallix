import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import { apiGet, apiPost } from "@/lib/api-client";
import type { ContactLedger, LedgerEntry, RecordEntryInput } from "@/types/api";

export const HISTORY_PAGE_SIZE = 10;

export function useContactLedger(contactId: string, page: number) {
  return useQuery({
    queryKey: ["ledger", contactId, { page }],
    queryFn: () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(HISTORY_PAGE_SIZE),
      });

      return apiGet<ContactLedger>(
        `/contacts/${encodeURIComponent(contactId)}/ledger?${params.toString()}`,
      );
    },
    // Keep the previous page on screen while the next one loads, but never one
    // that belongs to a different contact.
    placeholderData: (previousData, previousQuery) =>
      previousQuery?.queryKey[1] === contactId ? previousData : undefined,
  });
}

export function useRecordEntry(contactId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: RecordEntryInput) =>
      apiPost<LedgerEntry>(`/contacts/${encodeURIComponent(contactId)}/ledger`, input),
    onSuccess: () => invalidateLedgerData(queryClient, contactId),
  });
}

export function useReverseEntry(contactId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, description }: { id: string; description: string | null }) =>
      apiPost<LedgerEntry>(`/ledger/${encodeURIComponent(id)}/reverse`, { description }),
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