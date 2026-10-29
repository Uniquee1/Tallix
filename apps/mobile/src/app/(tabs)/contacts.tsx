import { useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { useRouter } from "expo-router";
import { ErrorView } from "../../components/ErrorView";
import { useContactsList } from "../../lib/queries";
import { colors } from "../../lib/theme";
import type { Contact } from "../../lib/types";
import { useDebouncedValue } from "../../lib/useDebouncedValue";

export default function ContactsScreen() {
  const router = useRouter();
  const [searchInput, setSearchInput] = useState("");
  const search = useDebouncedValue(searchInput.trim(), 300);
  const query = useContactsList(search);

  const contacts = query.data?.pages.flatMap((page) => page.items) ?? [];
  const total = query.data?.pages[0]?.totalCount ?? 0;

  return (
    <View style={styles.screen}>
      <TextInput
        style={styles.search}
        value={searchInput}
        onChangeText={setSearchInput}
        placeholder="Search by name, email or phone"
        autoCapitalize="none"
        autoCorrect={false}
        clearButtonMode="while-editing"
        returnKeyType="search"
      />

      {query.status === "pending" && (
        <View style={styles.centered}>
          <ActivityIndicator size="large" />
        </View>
      )}

      {query.status === "error" && (
        <ErrorView
          error={query.error}
          onRetry={() => {
            void query.refetch();
          }}
        />
      )}

      {query.status === "success" && (
        <FlatList
          data={contacts}
          keyExtractor={(contact) => contact.id}
          renderItem={({ item }) => <ContactRow contact={item} />}
          keyboardShouldPersistTaps="handled"
          style={query.isPlaceholderData ? styles.dimmed : undefined}
          ListHeaderComponent={
            <Text style={styles.count}>
              {total} {total === 1 ? "contact" : "contacts"}
            </Text>
          }
          ListEmptyComponent={
            <Text style={styles.empty}>
              {search === "" ? "No contacts yet." : `No contacts match “${search}”.`}
            </Text>
          }
          ListFooterComponent={
            query.isFetchingNextPage ? <ActivityIndicator style={styles.footer} /> : undefined
          }
          onEndReachedThreshold={0.5}
          onEndReached={() => {
            if (query.hasNextPage && !query.isFetchingNextPage) {
              void query.fetchNextPage();
            }
          }}
          refreshControl={
            <RefreshControl
              refreshing={query.isRefetching && !query.isFetchingNextPage}
              onRefresh={() => {
                void query.refetch();
              }}
            />
          }
        />
      )}
            <Pressable
        style={styles.fab}
        accessibilityLabel="Add contact"
        onPress={() => router.push("/contact-form")}
      >
        <Text style={styles.fabText}>+</Text>
      </Pressable>
    </View>
  );
}

function ContactRow({ contact }: { contact: Contact }) {
  const router = useRouter();
  const details = [contact.phone, contact.email].filter(Boolean).join(" · ");

  return (
    <Pressable
      style={styles.row}
      android_ripple={{ color: colors.border }}
      onPress={() => router.push({ pathname: "/contact/[id]", params: { id: contact.id } })}
    >
      <Text style={styles.name}>{contact.name}</Text>
      {details !== "" && <Text style={styles.muted}>{details}</Text>}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1 },
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
  search: {
    margin: 16,
    marginBottom: 8,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  dimmed: { opacity: 0.6 },
  count: { color: colors.muted, paddingHorizontal: 16, paddingBottom: 8 },
  empty: { color: colors.muted, textAlign: "center", padding: 32 },
  footer: { padding: 16 },
  row: {
    gap: 2,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  name: { fontSize: 16, fontWeight: "500", color: colors.text },
  muted: { color: colors.muted },
  fab: {
    position: "absolute",
    right: 20,
    bottom: 20,
    width: 56,
    height: 56,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 28,
    backgroundColor: colors.primary,
    elevation: 4,
  },
  fabText: { color: "#fff", fontSize: 28, lineHeight: 30 },
});