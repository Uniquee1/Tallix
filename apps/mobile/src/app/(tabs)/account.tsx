import { useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from "react-native";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "../../components/Button";
import { ErrorView } from "../../components/ErrorView";
import { LocalDataCard } from "../../components/LocalDataCard";
import { useAuth } from "../../lib/AuthContext";
import { wipeDatabase } from "../../lib/db/database";
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

    // Signing out always ends signed out, so whatever happened above,
    // nothing may stay behind on the phone.
    try {
      await wipeDatabase();
    } catch (e) {
      console.warn("Could not delete the local database:", e);
    }

    // Never let one person's cached data show up for the next.
    queryClient.clear();
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
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

      <LocalDataCard />

      <Button label="Log out" onPress={handleSignOut} />

      {error && <Text style={styles.error}>{error}</Text>}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { gap: 16, padding: 24 },
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
  error: { color: colors.danger, textAlign: "center" },
});