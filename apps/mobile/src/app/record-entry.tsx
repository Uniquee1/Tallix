import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Button } from "../components/Button";
import { Field } from "../components/Field";
import { isValidCalendarDate, transactionLabels } from "../lib/format";
import { useRecordEntry } from "../lib/queries";
import { colors } from "../lib/theme";
import type { TransactionType } from "../lib/types";

const TYPES: TransactionType[] = [
  "LENT",
  "BORROWED",
  "PAYMENT_RECEIVED",
  "PAYMENT_MADE",
  "ADJUSTMENT",
];

const HINTS: Record<TransactionType, string> = {
  LENT: "You gave them money, so they owe you more.",
  BORROWED: "You received money from them, so you owe them more.",
  PAYMENT_RECEIVED: "They paid you back.",
  PAYMENT_MADE: "You paid them back.",
  ADJUSTMENT: "A correction. Positive means they owe you more, negative means less.",
};

const MAX_AMOUNT = 1_000_000_000;

// The amount is checked as text first, so JavaScript never rounds what was typed.
function validate(type: TransactionType, amount: string, dueDate: string): string | null {
  const pattern = type === "ADJUSTMENT" ? /^-?\d+(\.\d{1,2})?$/ : /^\d+(\.\d{1,2})?$/;

  if (!pattern.test(amount.trim())) {
    return "Enter an amount with at most two decimal places.";
  }

  if (Number(amount) === 0) {
    return "The amount can’t be zero.";
  }

  if (Math.abs(Number(amount)) > MAX_AMOUNT) {
    return "That amount is too large.";
  }

  if (dueDate !== "" && !isValidCalendarDate(dueDate)) {
    return "Write the due date as YYYY-MM-DD, for example 2026-11-15.";
  }

  return null;
}

export default function RecordEntryScreen() {
  const { contactId } = useLocalSearchParams<{ contactId: string }>();
  const router = useRouter();
  const record = useRecordEntry(contactId);

  const [type, setType] = useState<TransactionType>("LENT");
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [description, setDescription] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);

  const allowsDueDate = type === "LENT" || type === "BORROWED";

  function handleSubmit() {
    const problem = validate(type, amount, allowsDueDate ? dueDate : "");

    if (problem) {
      setLocalError(problem);
      return;
    }

    setLocalError(null);
    record.mutate(
      {
        type,
        amount: Number(amount),
        occurredAt: null,
        dueDate: allowsDueDate && dueDate ? dueDate : null,
        description: description.trim() || null,
      },
      { onSuccess: () => router.back() },
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.chips}>
        {TYPES.map((value) => (
          <Pressable
            key={value}
            onPress={() => setType(value)}
            style={[styles.chip, type === value && styles.chipActive]}
          >
            <Text style={[styles.chipText, type === value && styles.chipTextActive]}>
              {transactionLabels[value]}
            </Text>
          </Pressable>
        ))}
      </View>

      <Text style={styles.hint}>{HINTS[type]}</Text>

      <Field
        label="Amount (₱)"
        value={amount}
        onChangeText={setAmount}
        keyboardType={type === "ADJUSTMENT" ? "numbers-and-punctuation" : "decimal-pad"}
      />

      {allowsDueDate && (
        <Field
          label="Due date (optional, YYYY-MM-DD)"
          value={dueDate}
          onChangeText={setDueDate}
          keyboardType="numbers-and-punctuation"
          maxLength={10}
        />
      )}

      <Field
        label="Description (optional)"
        value={description}
        onChangeText={setDescription}
        maxLength={500}
      />

      {(localError ?? record.error?.message) && (
        <Text style={styles.error}>{localError ?? record.error?.message}</Text>
      )}

      <Button
        label={record.isPending ? "Saving…" : "Record transaction"}
        onPress={handleSubmit}
        disabled={record.isPending}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: { gap: 16, padding: 16 },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  chip: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { color: colors.text },
  chipTextActive: { color: "#fff" },
  hint: { color: colors.muted },
  error: { color: colors.danger },
});