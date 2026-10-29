import { useState } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../lib/AuthContext";
import { isUserCancelled, messageOf } from "../lib/errors";
import { colors } from "../lib/theme";

export default function SignIn() {
  const { signIn } = useAuth();
  const [error, setError] = useState<string | null>(null);

  async function handleSignIn() {
    setError(null);

    try {
      await signIn();
    } catch (e) {
      if (!isUserCancelled(e)) setError(messageOf(e));
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Tallix</Text>
      <Text style={styles.muted}>Keep track of who owes you and who you owe.</Text>

      <Pressable onPress={handleSignIn} style={styles.button}>
        <Text style={styles.buttonText}>Log in</Text>
      </Pressable>

      {error && <Text style={styles.error}>{error}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16, padding: 24 },
  title: { fontSize: 32, fontWeight: "600", color: colors.text },
  muted: { color: colors.muted, textAlign: "center" },
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