import {
  ActivityIndicator,
  Alert,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { Stack, useLocalSearchParams, useRouter } from "expo-router";
import { Button } from "../../components/Button";
import { ErrorView } from "../../components/ErrorView";
import { ApiError } from "../../lib/api";
import {
  formatCalendarDate,
  formatDateTime,
  formatMoney,
  transactionLabels,
} from "../../lib/format";
import { useContact, useContactLedger, useReverseEntry } from "../../lib/queries";
import { colors } from "../../lib/theme";
import type { Contact, LedgerHistoryItem } from "../../lib/types";

export default function ContactScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const contact = useContact(id);
  const ledger = useContactLedger(id);
  const reverse = useReverseEntry(id);

  if (contact.status === "pending") {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (contact.status === "error") {
    if (contact.error instanceof ApiError && contact.error.status === 404) {
      return (
        <View style={styles.centered}>
          <Text style={styles.muted}>Contact not found.</Text>
        </View>
      );
    }

    return (
      <ErrorView
        error={contact.error}
        onRetry={() => {
          void contact.refetch();
        }}
      />
    );
  }

  const entries = ledger.data?.pages.flatMap((page) => page.history.items) ?? [];
  const balance = ledger.data?.pages[0]?.balance;

  function confirmReverse(item: LedgerHistoryItem) {
    Alert.alert("Reverse this entry?", "A new entry cancels it out. Nothing is deleted.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Reverse",
        style: "destructive",
        onPress: () =>
          reverse.mutate(
            { entryId: item.id, description: null },
            { onError: (error) => Alert.alert("Couldn’t reverse", error.message) },
          ),
      },
    ]);
  }

  return (
    <>
      <Stack.Screen options={{ title: contact.data.name }} />

      <FlatList
        data={entries}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <HistoryRow item={item} onReverse={() => confirmReverse(item)} />
        )}
        ListHeaderComponent={
          <Header
            contact={contact.data}
            balance={balance}
            onRecord={() => router.push({ pathname: "/record-entry", params: { contactId: id } })}
            onEdit={() => router.push({ pathname: "/contact-form", params: { id } })}
          />
        }
        ListEmptyComponent={
          ledger.status === "pending" ? (
            <ActivityIndicator style={styles.footer} />
          ) : ledger.status === "error" ? (
            <View style={styles.footer}>
              <Text style={styles.error}>{ledger.error.message}</Text>
              <Button
                label="Try again"
                onPress={() => {
                  void ledger.refetch();
                }}
              />
            </View>
          ) : (
            <Text style={styles.empty}>No transactions yet. Record the first one above.</Text>
          )
        }
        ListFooterComponent={
          ledger.isFetchingNextPage ? <ActivityIndicator style={styles.footer} /> : undefined
        }
        onEndReachedThreshold={0.5}
        onEndReached={() => {
          if (ledger.hasNextPage && !ledger.isFetchingNextPage) {
            void ledger.fetchNextPage();
          }
        }}
        refreshControl={
          <RefreshControl
            refreshing={ledger.isRefetching && !ledger.isFetchingNextPage}
            onRefresh={() => {
              void ledger.refetch();
            }}
          />
        }
      />
    </>
  );
}

function Header({
  contact,
  balance,
  onRecord,
  onEdit,
}: {
  contact: Contact;
  balance: number | undefined;
  onRecord: () => void;
  onEdit: () => void;
}) {
  const details = [contact.phone, contact.email].filter(Boolean).join(" · ");

  return (
    <View style={styles.header}>
      <View>
        <Text style={styles.name}>{contact.name}</Text>
        {details !== "" && <Text style={styles.muted}>{details}</Text>}
        {contact.notes && <Text style={styles.notes}>{contact.notes}</Text>}
      </View>

      <BalanceCard name={contact.name} balance={balance} />

      <View style={styles.actions}>
        <View style={styles.flex}>
          <Button label="Record transaction" onPress={onRecord} />
        </View>
        <Button label="Edit" variant="secondary" onPress={onEdit} />
      </View>

      <Text style={styles.sectionTitle}>History</Text>
    </View>
  );
}

function BalanceCard({ name, balance }: { name: string; balance: number | undefined }) {
  if (balance === undefined) {
    return (
      <View style={styles.card}>
        <Text style={styles.muted}>Loading balance…</Text>
      </View>
    );
  }

  if (balance === 0) {
    return (
      <View style={styles.card}>
        <Text style={styles.muted}>Balance</Text>
        <Text style={[styles.balance, { color: colors.muted }]}>All settled up</Text>
      </View>
    );
  }

  const owesYou = balance > 0;

  return (
    <View style={styles.card}>
      <Text style={styles.muted}>{owesYou ? `${name} owes you` : `You owe ${name}`}</Text>
      <Text style={[styles.balance, { color: owesYou ? colors.positive : colors.negative }]}>
        {formatMoney(Math.abs(balance))}
      </Text>
    </View>
  );
}

function HistoryRow({ item, onReverse }: { item: LedgerHistoryItem; onReverse: () => void }) {
  const isReversal = item.reversesEntryId !== null;
  const isReversed = item.reversedByEntryId !== null;
  const balanceColor =
    item.runningBalance > 0
      ? colors.positive
      : item.runningBalance < 0
        ? colors.negative
        : colors.muted;

  return (
    <View style={styles.row}>
      <View style={styles.rowTop}>
        <View style={styles.flex}>
          <Text style={styles.rowTitle}>
            {isReversal ? "Reversal" : transactionLabels[item.type]}
            {isReversed ? " (reversed)" : ""}
          </Text>
          <Text style={styles.muted}>
            {formatDateTime(item.occurredAt)}
            {item.dueDate ? ` · due ${formatCalendarDate(item.dueDate)}` : ""}
          </Text>
          {item.description ? <Text style={styles.description}>{item.description}</Text> : null}
        </View>

        <View style={styles.right}>
          <Text style={isReversed ? styles.struck : styles.rowTitle}>{formatMoney(item.amount)}</Text>
          <Text style={styles.small}>
            Balance <Text style={{ color: balanceColor }}>{formatMoney(item.runningBalance)}</Text>
          </Text>
        </View>
      </View>

      {!isReversal && !isReversed && (
        <Pressable onPress={onReverse}>
          <Text style={styles.link}>Reverse</Text>
        </Pressable>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
  flex: { flex: 1 },
  header: { gap: 16, padding: 16 },
  name: { fontSize: 24, fontWeight: "600", color: colors.text },
  notes: { color: colors.muted, marginTop: 4 },
  muted: { color: colors.muted },
  card: {
    gap: 4,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16,
  },
  balance: { fontSize: 26, fontWeight: "600" },
  actions: { flexDirection: "row", gap: 12 },
  sectionTitle: { fontSize: 18, fontWeight: "600", color: colors.text },
  row: {
    gap: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  rowTop: { flexDirection: "row", gap: 12 },
  rowTitle: { fontWeight: "500", color: colors.text },
  description: { color: colors.text },
  right: { alignItems: "flex-end" },
  small: { color: colors.muted, fontSize: 12 },
  struck: { color: colors.faint, textDecorationLine: "line-through" },
  link: { color: colors.text, textDecorationLine: "underline" },
  footer: { alignItems: "center", gap: 12, padding: 24 },
  error: { color: colors.danger, textAlign: "center" },
  empty: { color: colors.muted, textAlign: "center", padding: 32 },
});