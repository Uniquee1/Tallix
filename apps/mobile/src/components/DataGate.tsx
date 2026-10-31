import { useEffect, useRef, type ReactNode } from "react";
import { ActivityIndicator, StyleSheet, Text, View } from "react-native";
import { useHydrate, useLocalStats } from "../lib/localQueries";
import { colors } from "../lib/theme";
import { Button } from "./Button";

// Shows the app once the phone has data. The very first launch must download it
// (that needs the internet once). Every launch after that opens straight onto
// local data and refreshes quietly in the background.
export function DataGate({ children }: { children: ReactNode }) {
  const stats = useLocalStats();
  const hydrate = useHydrate();
  const sync = hydrate.mutate;
  const started = useRef(false);

  // Once per app start. Offline, this fails quietly, and that is fine.
  useEffect(() => {
    if (stats.status === "success" && !started.current) {
      started.current = true;
      sync();
    }
  }, [stats.status, sync]);

  if (stats.status === "pending") {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (stats.status === "error") {
    return (
      <View style={styles.centered}>
        <Text style={styles.error}>{stats.error.message}</Text>
      </View>
    );
  }

  if (stats.data.lastSnapshotAt === null) {
    if (hydrate.status === "error") {
      return (
        <View style={styles.centered}>
          <Text style={styles.title}>Couldn’t set up your ledger</Text>
          <Text style={styles.muted}>The first download needs an internet connection.</Text>
          <Text style={styles.error}>{hydrate.error.message}</Text>
          <Button label="Try again" onPress={() => sync()} />
        </View>
      );
    }

    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
        <Text style={styles.muted}>Setting up your ledger…</Text>
      </View>
    );
  }

  return <>{children}</>;
}

const styles = StyleSheet.create({
  centered: { flex: 1, alignItems: "center", justifyContent: "center", gap: 12, padding: 24 },
  title: { fontSize: 20, fontWeight: "600", color: colors.text },
  muted: { color: colors.muted, textAlign: "center" },
  error: { color: colors.danger, textAlign: "center" },
});