import { useState } from "react";
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from "react-native";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorView } from "../../components/ErrorView";
import { useAuth } from "../../lib/AuthContext";
import { messageOf } from "../../lib/errors";
import { useMe } from "../../lib/queries";
import { colors } from "../../lib/theme";

export default function AccountScreen() {
  const { signOut } = useAuth();
  const queryClient = useQueryClient();
  const me = useMe();
  const [error, setError] = useState<string | null>(null);

  async function handleSignOut() {
    setError(null);

    try {
      await signOut();
    } catch (e) {
      setError(messageOf(e));
    }

    // Never let one person's cached data show up for the next.
    queryClient.clear();
  }

  return (
    <View style={styles.container}>
      {me.status === "pending" && <ActivityIndicator />}

      {me.status === "error" && (
        <ErrorView
          error={me.error}
          onRetry={() => {
            void me.refetch();
          }}
        />
      )}

      {me.status === "success" && (
        <View style={styles.card}>
          <Text style={styles.muted}>Signed in as</Text>
          <Text style={styles.name}>{me.data.displayName}</Text>
          <Text>{me.data.email}</Text>
        </View>
      )}

      <Pressable onPress={handleSignOut} style={styles.button}>
        <Text style={styles.buttonText}>Log out</Text>
      </Pressable>

      {error && <Text style={styles.error}>{error}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16, padding: 24 },
  card: {
    alignItems: "center",
    gap: 4,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 20,
  },
  name: { fontSize: 20, fontWeight: "600", color: colors.text },
  muted: { color: colors.muted },
  button: {
    minWidth: 160,
    alignItems: "center",
    borderRadius: 8,
    backgroundColor: colors.primary,
    paddingHorizontal: 20,
    paddingVertical: 12,
  },
  buttonText: { color: "#fff", fontSize: 16 },
  error: { color: colors.danger, textAlign: "center" },
});