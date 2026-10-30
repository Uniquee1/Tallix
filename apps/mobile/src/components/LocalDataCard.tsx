import { ActivityIndicator, StyleSheet, Text, View } from "react-native";
import { formatDateTime } from "../lib/format";
import { useHydrate, useLocalStats } from "../lib/localQueries";
import { colors } from "../lib/theme";
import { Button } from "./Button";

export function LocalDataCard() {
  const stats = useLocalStats();
  const hydrate = useHydrate();

  return (
    <View style={styles.card}>
      <Text style={styles.title}>Local data (developer)</Text>

      {stats.status === "pending" && <ActivityIndicator />}

      {stats.status === "error" && <Text style={styles.error}>{stats.error.message}</Text>}

      {stats.status === "success" && (
        <View style={styles.stats}>
          <Text>Contacts: {stats.data.contacts}</Text>
          <Text>Ledger entries: {stats.data.entries}</Text>
          <Text>Changes waiting to sync: {stats.data.pending}</Text>
          <Text style={styles.muted}>
            Last download:{" "}
            {stats.data.lastSnapshotAt ? formatDateTime(stats.data.lastSnapshotAt) : "never"}
          </Text>
        </View>
      )}

      <Button
        label={hydrate.isPending ? "Downloading…" : "Download from server"}
        variant="secondary"
        disabled={hydrate.isPending}
        onPress={() => hydrate.mutate()}
      />

      {hydrate.status === "success" && (
        <Text style={styles.muted}>
          Downloaded {hydrate.data.contacts} contacts and {hydrate.data.entries} entries.
        </Text>
      )}

      {hydrate.status === "error" && <Text style={styles.error}>{hydrate.error.message}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    alignSelf: "stretch",
    gap: 10,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16,
  },
  title: { fontSize: 16, fontWeight: "600", color: colors.text },
  stats: { gap: 2 },
  muted: { color: colors.muted },
  error: { color: colors.danger },
});