import { Pressable, StyleSheet, Text, View } from "react-native";
import { ApiError } from "../lib/api";
import { colors } from "../lib/theme";

export function ErrorView({ error, onRetry }: { error: Error; onRetry: () => void }) {
  const signedOut = error instanceof ApiError && error.status === 401;

  return (
    <View style={styles.container}>
      <Text style={styles.message}>{error.message}</Text>

      {signedOut && (
        <Text style={styles.hint}>
          Your session may have expired. Log out from the Account tab and sign in again.
        </Text>
      )}

      <Pressable onPress={onRetry} style={styles.button}>
        <Text style={styles.buttonText}>Try again</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: "center", justifyContent: "center", gap: 12, padding: 24 },
  message: { color: colors.danger, textAlign: "center", fontSize: 16 },
  hint: { color: colors.muted, textAlign: "center" },
  button: {
    borderRadius: 8,
    backgroundColor: colors.primary,
    paddingHorizontal: 20,
    paddingVertical: 10,
  },
  buttonText: { color: "#fff", fontSize: 16 },
});