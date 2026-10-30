import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getLocalStats } from "./db/meta";
import { hydrateFromServer } from "./sync/hydrate";

export function useLocalStats() {
  return useQuery({ queryKey: ["local", "stats"], queryFn: getLocalStats });
}

export function useHydrate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: hydrateFromServer,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["local"] }),
  });
}