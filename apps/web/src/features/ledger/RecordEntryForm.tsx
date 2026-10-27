"use client";

import { useState } from "react";
import { localDateToInstant, transactionLabels } from "@/lib/format";
import type { RecordEntryInput, TransactionType } from "@/types/api";

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

type Props = {
  pending: boolean;
  error: Error | null;
  onSubmit: (input: RecordEntryInput) => void;
  onCancel: () => void;
};

export function RecordEntryForm({ pending, error, onSubmit, onCancel }: Props) {
  const [type, setType] = useState<TransactionType>("LENT");
  const [amount, setAmount] = useState("");
  const [date, setDate] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [description, setDescription] = useState("");

  const allowsDueDate = type === "LENT" || type === "BORROWED";
  const isAdjustment = type === "ADJUSTMENT";

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({
          type,
          amount: Number(amount),
          occurredAt: date ? localDateToInstant(date) : null,
          dueDate: allowsDueDate && dueDate ? dueDate : null,
          description: description.trim() || null,
        });
      }}
      className="space-y-3 rounded-lg border bg-white p-4"
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm">
          <span className="mb-1 block text-gray-600">Type</span>
          <select
            value={type}
            onChange={(event) => setType(event.target.value as TransactionType)}
            className="w-full rounded border bg-white px-3 py-2"
          >
            {TYPES.map((value) => (
              <option key={value} value={value}>
                {transactionLabels[value]}
              </option>
            ))}
          </select>
        </label>

        <label className="block text-sm">
          <span className="mb-1 block text-gray-600">Amount (₱)</span>
          <input
            required
            type="number"
            inputMode="decimal"
            step="0.01"
            min={isAdjustment ? "-1000000000" : "0.01"}
            max="1000000000"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            className="w-full rounded border px-3 py-2"
          />
        </label>
      </div>

      <p className="text-sm text-gray-500">{HINTS[type]}</p>

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm">
          <span className="mb-1 block text-gray-600">Date (optional, defaults to now)</span>
          <input
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            className="w-full rounded border px-3 py-2"
          />
        </label>

        {allowsDueDate && (
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">Due date (optional)</span>
            <input
              type="date"
              value={dueDate}
              onChange={(event) => setDueDate(event.target.value)}
              className="w-full rounded border px-3 py-2"
            />
          </label>
        )}
      </div>

      <label className="block text-sm">
        <span className="mb-1 block text-gray-600">Description (optional)</span>
        <input
          maxLength={500}
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          className="w-full rounded border px-3 py-2"
        />
      </label>

      {error && <p className="text-sm text-red-700">{error.message}</p>}

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={pending}
          className="rounded bg-gray-900 px-4 py-2 text-white hover:bg-gray-700 disabled:opacity-50"
        >
          {pending ? "Saving…" : "Record transaction"}
        </button>
        <button type="button" onClick={onCancel} className="rounded border px-4 py-2 hover:bg-gray-100">
          Cancel
        </button>
      </div>
    </form>
  );
}