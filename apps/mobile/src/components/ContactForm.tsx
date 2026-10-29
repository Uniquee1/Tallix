import { useState } from "react";
import { ScrollView, StyleSheet, Text } from "react-native";
import { colors } from "../lib/theme";
import type { Contact, ContactInput } from "../lib/types";
import { Button } from "./Button";
import { Field } from "./Field";

// These mirror the limits in the API's domain, for convenience only.
// The API stays the authority and rejects anything invalid.
const LIMITS = { name: 200, phone: 50, email: 320, notes: 2000 };

type Props = {
  initial?: Contact;
  submitLabel: string;
  pending: boolean;
  error: Error | null;
  onSubmit: (values: ContactInput) => void;
  onDelete?: () => void;
};

export function ContactForm({ initial, submitLabel, pending, error, onSubmit, onDelete }: Props) {
  // The form starts from `initial` when it appears, so there is nothing to keep in sync.
  const [name, setName] = useState(initial?.name ?? "");
  const [phone, setPhone] = useState(initial?.phone ?? "");
  const [email, setEmail] = useState(initial?.email ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");
  const [localError, setLocalError] = useState<string | null>(null);

  function handleSubmit() {
    if (name.trim() === "") {
      setLocalError("Name is required.");
      return;
    }

    setLocalError(null);
    onSubmit({
      name,
      phone: phone || null,
      email: email || null,
      notes: notes || null,
    });
  }

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Field label="Name" value={name} onChangeText={setName} maxLength={LIMITS.name} />
      <Field
        label="Phone"
        value={phone}
        onChangeText={setPhone}
        maxLength={LIMITS.phone}
        keyboardType="phone-pad"
      />
      <Field
        label="Email"
        value={email}
        onChangeText={setEmail}
        maxLength={LIMITS.email}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Field
        label="Notes"
        value={notes}
        onChangeText={setNotes}
        maxLength={LIMITS.notes}
        multiline
      />

      {(localError ?? error?.message) && <Text style={styles.error}>{localError ?? error?.message}</Text>}

      <Button label={pending ? "Saving…" : submitLabel} onPress={handleSubmit} disabled={pending} />
      {onDelete && <Button label="Delete contact" variant="danger" onPress={onDelete} />}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: { gap: 16, padding: 16 },
  error: { color: colors.danger },
});