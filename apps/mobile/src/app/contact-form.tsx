import { ActivityIndicator, Alert, StyleSheet, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { ContactForm } from "../components/ContactForm";
import { ErrorView } from "../components/ErrorView";
import { useContact, useCreateContact, useDeleteContact, useUpdateContact } from "../lib/queries";
import type { ContactInput } from "../lib/types";

// Without an id this creates a contact. With an id it edits that contact.
export default function ContactFormScreen() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const router = useRouter();
  const contact = useContact(id);
  const create = useCreateContact();
  const update = useUpdateContact();
  const remove = useDeleteContact();

  if (id !== undefined && contact.status === "pending") {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (id !== undefined && contact.status === "error") {
    return (
      <ErrorView
        error={contact.error}
        onRetry={() => {
          void contact.refetch();
        }}
      />
    );
  }

  const editing = id !== undefined && contact.status === "success" ? contact.data : undefined;

  function handleSubmit(values: ContactInput) {
    if (editing) {
      update.mutate({ id: editing.id, input: values }, { onSuccess: () => router.back() });
    } else {
      create.mutate(values, { onSuccess: () => router.back() });
    }
  }

  function handleDelete() {
    if (!editing) return;

    Alert.alert(`Delete ${editing.name}?`, "This can’t be undone.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Delete",
        style: "destructive",
        onPress: () =>
          remove.mutate(editing.id, {
            // Back out of this form and the page of the contact that no longer exists.
            onSuccess: () => router.dismissAll(),
            onError: (error) => Alert.alert("Couldn’t delete", error.message),
          }),
      },
    ]);
  }

  return (
    <ContactForm
      initial={editing}
      submitLabel={editing ? "Save changes" : "Add contact"}
      pending={create.isPending || update.isPending}
      error={create.error ?? update.error}
      onSubmit={handleSubmit}
      onDelete={editing ? handleDelete : undefined}
    />
  );
}

const styles = StyleSheet.create({
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
});