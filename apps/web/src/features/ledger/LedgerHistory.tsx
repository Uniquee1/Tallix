"use client";

import { useState } from "react";
import { formatCalendarDate, formatDateTime, formatMoney, transactionLabels } from "@/lib/format";
import type { LedgerHistoryItem } from "@/types/api";
import { useReverseEntry } from "./api";

export function LedgerHistory({
  contactId,
  items,
}: {
  contactId: string;
  items: LedgerHistoryItem[];
}) {
  const reverse = useReverseEntry(contactId);
  const [reversingId, setReversingId] = useState<string | null>(null);

  return (
    <ul className="divide-y rounded-lg border bg-white">
      {items.map((item) => {
        const isReversal = item.reversesEntryId !== null;
        const isReversed = item.reversedByEntryId !== null;
        const balanceColor =
          item.runningBalance > 0
            ? "text-emerald-700"
            : item.runningBalance < 0
              ? "text-rose-700"
              : "text-gray-600";

        return (
          <li key={item.id} className="p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="min-w-0">
                <p className="font-medium">
                  {isReversal ? "Reversal" : transactionLabels[item.type]}
                  {isReversed && (
                    <span className="ml-2 rounded bg-gray-200 px-2 py-0.5 text-xs font-normal text-gray-700">
                      Reversed
                    </span>
                  )}
                </p>
                <p className="text-sm text-gray-500">
                  {formatDateTime(item.occurredAt)}
                  {item.dueDate && ` · due ${formatCalendarDate(item.dueDate)}`}
                </p>
                {item.description && <p className="text-sm text-gray-600">{item.description}</p>}
              </div>

              <div className="shrink-0 text-right">
                <p className={isReversed ? "text-gray-400 line-through" : ""}>
                  {formatMoney(item.amount)}
                </p>
                <p className="text-sm text-gray-500">
                  Balance <span className={balanceColor}>{formatMoney(item.runningBalance)}</span>
                </p>
              </div>
            </div>

            {!isReversal && !isReversed && reversingId !== item.id && (
              <button
                type="button"
                onClick={() => {
                  reverse.reset();
                  setReversingId(item.id);
                }}
                className="mt-2 text-sm underline"
              >
                Reverse
              </button>
            )}

            {reversingId === item.id && (
              <ReverseConfirm
                pending={reverse.isPending}
                error={reverse.error}
                onConfirm={(description) =>
                  reverse.mutate(
                    { id: item.id, description },
                    { onSuccess: () => setReversingId(null) },
                  )
                }
                onCancel={() => setReversingId(null)}
              />
            )}
          </li>
        );
      })}
    </ul>
  );
}

function ReverseConfirm({
  pending,
  error,
  onConfirm,
  onCancel,
}: {
  pending: boolean;
  error: Error | null;
  onConfirm: (description: string | null) => void;
  onCancel: () => void;
}) {
  const [reason, setReason] = useState("");

  return (
    <div className="mt-3 rounded bg-amber-50 p-3 text-sm">
      <p>Reverse this entry? A new entry cancels it out. Nothing is deleted.</p>

      <input
        value={reason}
        maxLength={500}
        placeholder="Reason (optional)"
        onChange={(event) => setReason(event.target.value)}
        className="mt-2 w-full rounded border bg-white px-3 py-2"
      />

      {error && <p className="mt-1 text-red-700">{error.message}</p>}

      <div className="mt-2 flex gap-3">
        <button
          type="button"
          disabled={pending}
          onClick={() => onConfirm(reason.trim() || null)}
          className="rounded bg-red-600 px-3 py-1 text-white disabled:opacity-50"
        >
          {pending ? "Reversing…" : "Reverse entry"}
        </button>
        <button type="button" onClick={onCancel} className="rounded border bg-white px-3 py-1">
          Cancel
        </button>
      </div>
    </div>
  );
}