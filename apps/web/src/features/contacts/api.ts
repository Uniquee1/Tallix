import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api-client";
import type { Contact, ContactInput, PagedResult } from "@/types/api";

export const CONTACTS_PAGE_SIZE = 10;

export function useContacts(search: string, page: number) {
  return useQuery({
    // Every search + page combination is cached separately under the "contacts" prefix.
    queryKey: ["contacts", { search, page }],
    queryFn: () => {
      const params = new URLSearchParams({
        search,
        page: String(page),
        pageSize: String(CONTACTS_PAGE_SIZE),
      });

      return apiGet<PagedResult<Contact>>(`/contacts?${params.toString()}`);
    },
    // Keep showing the previous page while the next one loads, so the list doesn't flash.
    placeholderData: keepPreviousData,
  });
}

export function useContact(id: string) {
  return useQuery({
    queryKey: ["contact", id],
    queryFn: () => apiGet<Contact>(`/contacts/${encodeURIComponent(id)}`),
  });
}

export function useCreateContact() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ContactInput) => apiPost<Contact>("/contacts", input),
    // Marks every cached contacts list as out of date, so the visible one refetches.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["contacts"] }),
  });
}

export function useUpdateContact() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ContactInput }) =>
      apiPut<Contact>(`/contacts/${encodeURIComponent(id)}`, input),
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
    mutationFn: (id: string) => apiDelete(`/contacts/${encodeURIComponent(id)}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["contacts"] }),
  });
}