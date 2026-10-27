"use client";

import { useQuery } from "@tanstack/react-query";
import { ErrorMessage } from "@/components/ErrorMessage";
import { apiGet } from "@/lib/api-client";
import {
  formatCalendarDate,
  formatDateTime,
  formatMoney,
  localToday,
  transactionLabels,
} from "@/lib/format";
import type { Dashboard, DashboardDueGroup, DashboardTransaction } from "@/types/api";

export function DashboardView() {
  const query = useQuery({
    queryKey: ["dashboard"],
    // The date is read here, when the request is made, never while rendering.
    queryFn: () => apiGet<Dashboard>(`/dashboard?asOf=${localToday()}`),
  });

  if (query.status === "pending") {
    return <p className="text-gray-500">Loading your dashboard…</p>;
  }

  if (query.status === "error") {
    return (
      <ErrorMessage
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
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-gray-500">
          As of {formatCalendarDate(dashboard.asOf)} · {contacts} active{" "}
          {contacts === 1 ? "contact" : "contacts"}
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Owed to me" value={formatMoney(dashboard.totalOwedToMe)} tone="positive" />
        <StatCard label="I owe" value={formatMoney(dashboard.totalIOwe)} tone="negative" />
        <StatCard
          label="Net"
          value={formatMoney(dashboard.netBalance)}
          tone={dashboard.netBalance >= 0 ? "positive" : "negative"}
        />
      </div>

      {dashboard.recentTransactions.length === 0 ? (
        <EmptyState />
      ) : (
        <>
          <div className="grid gap-6 lg:grid-cols-2">
            <DueSection title="Overdue" group={dashboard.overdue} emptyText="Nothing is overdue." />
            <DueSection
              title="Upcoming (next 7 days)"
              group={dashboard.upcoming}
              emptyText="Nothing due soon."
            />
          </div>

          <RecentTransactions transactions={dashboard.recentTransactions} />
        </>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: "positive" | "negative";
}) {
  const color = tone === "positive" ? "text-emerald-700" : "text-rose-700";

  return (
    <div className="rounded-lg border bg-white p-4">
      <p className="text-sm text-gray-500">{label}</p>
      <p className={`mt-1 text-2xl font-semibold ${color}`}>{value}</p>
    </div>
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
    <section className="rounded-lg border bg-white p-4">
      <h2 className="font-medium">{title}</h2>

      {group.count === 0 ? (
        <p className="mt-2 text-sm text-gray-500">{emptyText}</p>
      ) : (
        <>
          <p className="mt-1 text-sm text-gray-500">
            {group.owedToMeTotal > 0 && <>{formatMoney(group.owedToMeTotal)} owed to you</>}
            {group.owedToMeTotal > 0 && group.iOweTotal > 0 && " · "}
            {group.iOweTotal > 0 && <>{formatMoney(group.iOweTotal)} you owe</>}
          </p>

          <ul className="mt-3 divide-y">
            {group.items.map((item) => (
              <li key={item.entryId} className="flex items-baseline justify-between gap-4 py-2">
                <div>
                  <p className="font-medium">{item.contactName}</p>
                  <p className="text-sm text-gray-500">
                    {item.direction === "OWED_TO_ME" ? "owes you" : "you owe"} · due{" "}
                    {formatCalendarDate(item.dueDate)}
                  </p>
                </div>
                <p className="font-medium">{formatMoney(item.outstanding)}</p>
              </li>
            ))}
          </ul>

          {group.count > group.items.length && (
            <p className="mt-2 text-sm text-gray-500">
              and {group.count - group.items.length} more
            </p>
          )}
        </>
      )}
    </section>
  );
}

function RecentTransactions({ transactions }: { transactions: DashboardTransaction[] }) {
  return (
    <section className="rounded-lg border bg-white p-4">
      <h2 className="font-medium">Recent transactions</h2>

      <ul className="mt-3 divide-y">
        {transactions.map((transaction) => (
          <li
            key={transaction.entryId}
            className="flex items-baseline justify-between gap-4 py-2"
          >
            <div>
              <p className="font-medium">{transaction.contactName}</p>
              <p className="text-sm text-gray-500">
                {transaction.reversesEntryId ? "Reversal" : transactionLabels[transaction.type]} ·{" "}
                {formatDateTime(transaction.occurredAt)}
              </p>
            </div>
            <p>{formatMoney(transaction.amount)}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}

function EmptyState() {
  return (
    <div className="rounded-lg border border-dashed bg-white p-8 text-center">
      <p className="font-medium">Nothing recorded yet</p>
      <p className="mt-1 text-sm text-gray-500">
        Once you add contacts and record transactions, your totals and due dates appear here.
      </p>
    </div>
  );
}