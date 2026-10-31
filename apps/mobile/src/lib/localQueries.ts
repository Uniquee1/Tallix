import { useCallback } from "react";
import { Alert } from "react-native";
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
    // Every screen reads the local database, and it just changed.
    onSuccess: () => queryClient.invalidateQueries(),
  });
}

// Pull-to-refresh means "sync": bring this phone up to date with the server.
export function usePullToSync() {
  const { mutate, isPending } = useHydrate();

  const refresh = useCallback(() => {
    mutate(undefined, {
      onError: () =>
        Alert.alert("Couldn’t reach the server", "You’re seeing what’s saved on this phone."),
    });
  }, [mutate]);

  return { refresh, refreshing: isPending };
}