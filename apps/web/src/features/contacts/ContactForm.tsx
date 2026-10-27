"use client";

import { useState } from "react";
import type { Contact, ContactInput } from "@/types/api";

// These mirror the limits in the API's domain, for convenience only.
// The API stays the authority and rejects anything invalid.
const LIMITS = { name: 200, phone: 50, email: 320, notes: 2000 };

type Props = {
  initial?: Contact;
  submitLabel: string;
  pending: boolean;
  error: Error | null;
  onSubmit: (values: ContactInput) => void;
  onCancel: () => void;
};

export function ContactForm({ initial, submitLabel, pending, error, onSubmit, onCancel }: Props) {
  // The form starts from `initial` when it appears. Opening it again later
  // mounts a fresh copy, so there is nothing to keep in sync.
  const [name, setName] = useState(initial?.name ?? "");
  const [phone, setPhone] = useState(initial?.phone ?? "");
  const [email, setEmail] = useState(initial?.email ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({
          name,
          phone: phone || null,
          email: email || null,
          notes: notes || null,
        });
      }}
      className="space-y-3 rounded-lg border bg-white p-4"
    >
      <Field label="Name">
        <input
          required
          maxLength={LIMITS.name}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="w-full rounded border px-3 py-2"
        />
      </Field>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Phone">
          <input
            type="tel"
            maxLength={LIMITS.phone}
            value={phone}
            onChange={(event) => setPhone(event.target.value)}
            className="w-full rounded border px-3 py-2"
          />
        </Field>

        <Field label="Email">
          <input
            type="email"
            maxLength={LIMITS.email}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="w-full rounded border px-3 py-2"
          />
        </Field>
      </div>

      <Field label="Notes">
        <textarea
          rows={3}
          maxLength={LIMITS.notes}
          value={notes}
          onChange={(event) => setNotes(event.target.value)}
          className="w-full rounded border px-3 py-2"
        />
      </Field>

      {error && <p className="text-sm text-red-700">{error.message}</p>}

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={pending}
          className="rounded bg-gray-900 px-4 py-2 text-white hover:bg-gray-700 disabled:opacity-50"
        >
          {pending ? "Saving…" : submitLabel}
        </button>
        <button type="button" onClick={onCancel} className="rounded border px-4 py-2 hover:bg-gray-100">
          Cancel
        </button>
      </div>
    </form>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block text-gray-600">{label}</span>
      {children}
    </label>
  );
}