import { useState } from "react";
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from "react-native";

// EXPO_PUBLIC_* values are copied into the app bundle. Never put a secret in one.
const API_URL = process.env.EXPO_PUBLIC_API_URL;

export default function Index() {
  const [result, setResult] = useState("Not checked yet.");
  const [busy, setBusy] = useState(false);

  async function checkApi() {
    if (!API_URL) {
      setResult("EXPO_PUBLIC_API_URL is not set.");
      return;
    }

    setBusy(true);

    // A phone request has no useful default timeout, so we set our own.
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 8000);

    try {
      const response = await fetch(`${API_URL}/health`, { signal: controller.signal });
      setResult(`${response.status} ${await response.text()}`);
    } catch (error) {
      setResult(`Failed: ${error instanceof Error ? error.message : String(error)}`);
    } finally {
      clearTimeout(timer);
      setBusy(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Tallix</Text>
      <Text style={styles.muted}>API: {API_URL ?? "(not set)"}</Text>

      <Pressable onPress={checkApi} disabled={busy} style={styles.button}>
        {busy ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Check API</Text>}
      </Pressable>

      <Text style={styles.result}>{result}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16, padding: 24 },
  title: { fontSize: 32, fontWeight: "600" },
  muted: { color: "#6b7280" },
  button: {
    minWidth: 160,
    alignItems: "center",
    borderRadius: 8,
    backgroundColor: "#111827",
    paddingHorizontal: 20,
    paddingVertical: 12,
  },
  buttonText: { color: "#fff", fontSize: 16 },
  result: { fontSize: 16 },
});