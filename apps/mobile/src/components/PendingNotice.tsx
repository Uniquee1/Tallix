import { StyleSheet, Text, View } from "react-native";
import { useLocalStats } from "../lib/localQueries";
import { colors } from "../lib/theme";

// "☁ 3 changes waiting to sync", shown only when there are some.
export function PendingNotice() {
  const stats = useLocalStats();

  if (stats.status !== "success" || stats.data.pending === 0) {
    return null;
  }

  const count = stats.data.pending;

  return (
    <View style={styles.box}>
      <Text style={styles.text}>
        ☁ {count} {count === 1 ? "change" : "changes"} waiting to sync
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  box: {
    borderRadius: 8,
    backgroundColor: "#fef3c7",
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  text: { color: "#92400e" },
});