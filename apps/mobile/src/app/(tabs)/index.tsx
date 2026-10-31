import { ActivityIndicator, RefreshControl, ScrollView, StyleSheet, Text, View } from "react-native";
import { ErrorView } from "../../components/ErrorView";
import {
  formatCalendarDate,
  formatDateTime,
  formatMoney,
  transactionLabels,
} from "../../lib/format";
import { useDashboard } from "../../lib/queries";
import { colors } from "../../lib/theme";
import type { DashboardDueGroup, DashboardTransaction } from "../../lib/types";
import { usePullToSync } from "../../lib/localQueries";

export default function DashboardScreen() {
  const query = useDashboard();

  const { refresh, refreshing } = usePullToSync();

  if (query.status === "pending") {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (query.status === "error") {
    return (
      <ErrorView
        error={query.error}
        onRetry={() => {
          void query.refetch();
        }}
      />
    );
  }

  const dashboard = query.data;
  const contacts = dashboard.activeContactCount;

  return (
    <ScrollView
      contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
    >
      <Text style={styles.muted}>
        As of {formatCalendarDate(dashboard.asOf)} · {contacts} active{" "}
        {contacts === 1 ? "contact" : "contacts"}
      </Text>

      <View style={styles.row}>
        <StatCard label="Owed to me" value={formatMoney(dashboard.totalOwedToMe)} color={colors.positive} />
        <StatCard label="I owe" value={formatMoney(dashboard.totalIOwe)} color={colors.negative} />
      </View>

      <StatCard
        label="Net"
        value={formatMoney(dashboard.netBalance)}
        color={dashboard.netBalance >= 0 ? colors.positive : colors.negative}
      />

      {dashboard.recentTransactions.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyTitle}>Nothing recorded yet</Text>
          <Text style={styles.muted}>
            Once you add contacts and record transactions, your totals and due dates appear here.
          </Text>
        </View>
      ) : (
        <>
          <DueSection title="Overdue" group={dashboard.overdue} emptyText="Nothing is overdue." />
          <DueSection
            title="Upcoming (next 7 days)"
            group={dashboard.upcoming}
            emptyText="Nothing due soon."
          />
          <RecentSection transactions={dashboard.recentTransactions} />
        </>
      )}
    </ScrollView>
  );
}

function StatCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <View style={[styles.card, styles.stat]}>
      <Text style={styles.muted}>{label}</Text>
      <Text style={[styles.statValue, { color }]}>{value}</Text>
    </View>
  );
}

function DueSection({
  title,
  group,
  emptyText,
}: {
  title: string;
  group: DashboardDueGroup;
  emptyText: string;
}) {
  return (
    <View style={styles.card}>
      <Text style={styles.sectionTitle}>{title}</Text>

      {group.count === 0 ? (
        <Text style={styles.muted}>{emptyText}</Text>
      ) : (
        <>
          <Text style={styles.muted}>
            {[
              group.owedToMeTotal > 0 ? `${formatMoney(group.owedToMeTotal)} owed to you` : null,
              group.iOweTotal > 0 ? `${formatMoney(group.iOweTotal)} you owe` : null,
            ]
              .filter(Boolean)
              .join(" · ")}
          </Text>

          {group.items.map((item) => (
            <View key={item.entryId} style={styles.listRow}>
              <View style={styles.flex}>
                <Text style={styles.rowTitle}>{item.contactName}</Text>
                <Text style={styles.muted}>
                  {item.direction === "OWED_TO_ME" ? "owes you" : "you owe"} · due{" "}
                  {formatCalendarDate(item.dueDate)}
                </Text>
              </View>
              <Text style={styles.rowTitle}>{formatMoney(item.outstanding)}</Text>
            </View>
          ))}

          {group.count > group.items.length && (
            <Text style={styles.muted}>and {group.count - group.items.length} more</Text>
          )}
        </>
      )}
    </View>
  );
}

function RecentSection({ transactions }: { transactions: DashboardTransaction[] }) {
  return (
    <View style={styles.card}>
      <Text style={styles.sectionTitle}>Recent transactions</Text>

      {transactions.map((transaction) => (
        <View key={transaction.entryId} style={styles.listRow}>
          <View style={styles.flex}>
            <Text style={styles.rowTitle}>{transaction.contactName}</Text>
            <Text style={styles.muted}>
              {transaction.reversesEntryId ? "Reversal" : transactionLabels[transaction.type]} ·{" "}
              {formatDateTime(transaction.occurredAt)}
            </Text>
          </View>
          <Text>{formatMoney(transaction.amount)}</Text>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
  content: { gap: 12, padding: 16 },
  row: { flexDirection: "row", gap: 12 },
  flex: { flex: 1 },
  card: {
    gap: 6,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16,
  },
  stat: { flex: 1 },
  statValue: { fontSize: 22, fontWeight: "600" },
  muted: { color: colors.muted },
  sectionTitle: { fontSize: 16, fontWeight: "600", color: colors.text },
  rowTitle: { fontWeight: "500", color: colors.text },
  listRow: { flexDirection: "row", alignItems: "center", gap: 12, paddingVertical: 6 },
  empty: {
    alignItems: "center",
    gap: 6,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    borderStyle: "dashed",
    backgroundColor: colors.surface,
    padding: 24,
  },
  emptyTitle: { fontWeight: "600", color: colors.text },
});