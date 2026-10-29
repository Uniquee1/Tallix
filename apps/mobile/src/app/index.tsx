import { useState, type ReactNode } from "react";
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from "react-native";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { apiGet } from "../lib/api";
import { useAuth } from "../lib/AuthContext";
import type { User } from "../lib/types";

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

// Closing the login browser is not an error worth showing.
function isUserCancelled(error: unknown): boolean {
  return messageOf(error).includes("user_cancelled");
}

export default function Index() {
  const { status, signIn, signOut } = useAuth();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  async function handleSignIn() {
    setError(null);

    try {
      await signIn();
    } catch (e) {
      if (!isUserCancelled(e)) setError(messageOf(e));
    }
  }

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

  if (status === "loading") {
    return (
      <Centered>
        <ActivityIndicator size="large" />
      </Centered>
    );
  }

  if (status === "signedOut") {
    return (
      <Centered>
        <Text style={styles.title}>Tallix</Text>
        <Text style={styles.muted}>Keep track of who owes you and who you owe.</Text>
        <Button label="Log in" onPress={handleSignIn} />
        {error && <Text style={styles.error}>{error}</Text>}
      </Centered>
    );
  }

  return (
    <Centered>
      <Text style={styles.title}>Tallix</Text>
      <SignedIn />
      <Button label="Log out" onPress={handleSignOut} />
      {error && <Text style={styles.error}>{error}</Text>}
    </Centered>
  );
}

function SignedIn() {
  const me = useQuery({ queryKey: ["me"], queryFn: () => apiGet<User>("/me") });

  if (me.status === "pending") {
    return <ActivityIndicator />;
  }

  if (me.status === "error") {
    return <Text style={styles.error}>{me.error.message}</Text>;
  }

  return (
    <View style={styles.card}>
      <Text style={styles.muted}>Signed in as</Text>
      <Text style={styles.name}>{me.data.displayName}</Text>
      <Text>{me.data.email}</Text>
      <Text style={styles.small}>id {me.data.id}</Text>
    </View>
  );
}

function Centered({ children }: { children: ReactNode }) {
  return <View style={styles.container}>{children}</View>;
}

function Button({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable onPress={onPress} style={styles.button}>
      <Text style={styles.buttonText}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16, padding: 24 },
  title: { fontSize: 32, fontWeight: "600" },
  muted: { color: "#6b7280" },
  small: { color: "#9ca3af", fontSize: 11 },
  name: { fontSize: 20, fontWeight: "600" },
  card: {
    alignItems: "center",
    gap: 4,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "#e5e7eb",
    padding: 16,
  },
  button: {
    minWidth: 160,
    alignItems: "center",
    borderRadius: 8,
    backgroundColor: "#111827",
    paddingHorizontal: 20,
    paddingVertical: 12,
  },
  buttonText: { color: "#fff", fontSize: 16 },
  error: { color: "#b91c1c", textAlign: "center" },
});