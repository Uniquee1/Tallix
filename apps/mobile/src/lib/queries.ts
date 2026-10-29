import { keepPreviousData, useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { localToday } from "./format";
import { repositories } from "./repositories";

export const CONTACTS_PAGE_SIZE = 20;

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