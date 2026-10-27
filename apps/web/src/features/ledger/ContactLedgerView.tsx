"use client";

import { useState } from "react";
import Link from "next/link";
import { ErrorMessage } from "@/components/ErrorMessage";
import { Pagination } from "@/components/Pagination";
import { useContact } from "@/features/contacts/api";
import { ApiError } from "@/lib/api-client";
import { formatMoney } from "@/lib/format";
import { useContactLedger, useRecordEntry } from "./api";
import { LedgerHistory } from "./LedgerHistory";
import { RecordEntryForm } from "./RecordEntryForm";

export function ContactLedgerView({ contactId }: { contactId: string }) {
  const [page, setPage] = useState(1);
  const [recording, setRecording] = useState(false);

  const contact = useContact(contactId);
  const ledger = useContactLedger(contactId, page);
  const record = useRecordEntry(contactId);

  if (contact.status === "pending") {
    return <p className="text-gray-500">Loading…</p>;
  }

  if (contact.status === "error") {
    const notFound = contact.error instanceof ApiError && contact.error.status === 404;

    return (
      <div className="space-y-4">
        <BackLink />
        {notFound ? (
          <p className="rounded border bg-white p-4">Contact not found.</p>
        ) : (
          <ErrorMessage
            error={contact.error}
            onRetry={() => {
              void contact.refetch();
            }}
          />
        )}
      </div>
    );
  }

  const { name, phone, email, notes } = contact.data;

  return (
    <div className="space-y-6">
      <BackLink />

      <div>
        <h1 className="text-2xl font-semibold">{name}</h1>
        {(phone || email) && (
          <p className="text-gray-600">{[phone, email].filter(Boolean).join(" · ")}</p>
        )}
        {notes && <p className="mt-1 text-sm text-gray-500">{notes}</p>}
      </div>

      {ledger.status === "success" && <BalanceCard name={name} balance={ledger.data.balance} />}

      {recording ? (
        <RecordEntryForm
          pending={record.isPending}
          error={record.error}
          onSubmit={(input) =>
            record.mutate(input, {
              onSuccess: () => {
                setRecording(false);
                // New entries appear at the top of the first page.
                setPage(1);
              },
            })
          }
          onCancel={() => setRecording(false)}
        />
      ) : (
        <button
          type="button"
          onClick={() => {
            record.reset();
            setRecording(true);
          }}
          className="rounded bg-gray-900 px-4 py-2 text-white hover:bg-gray-700"
        >
          Record transaction
        </button>
      )}

      <section className="space-y-3">
        <h2 className="text-lg font-medium">History</h2>

        {ledger.status === "pending" && <p className="text-gray-500">Loading history…</p>}

        {ledger.status === "error" && (
          <ErrorMessage
            error={ledger.error}
            onRetry={() => {
              void ledger.refetch();
            }}
          />
        )}

        {ledger.status === "success" && (
          <>
            {ledger.data.history.totalCount === 0 ? (
              <div className="rounded-lg border border-dashed bg-white p-8 text-center text-gray-600">
                No transactions yet. Record the first one above.
              </div>
            ) : (
              <>
                <p className="text-sm text-gray-500">
                  Balance: positive means they owe you, negative means you owe them.
                </p>
                <div className={ledger.isPlaceholderData ? "opacity-60" : ""}>
                  <LedgerHistory contactId={contactId} items={ledger.data.history.items} />
                </div>
              </>
            )}

            <Pagination
              page={ledger.data.history.page}
              totalPages={Math.max(
                1,
                Math.ceil(ledger.data.history.totalCount / ledger.data.history.pageSize),
              )}
              onChange={setPage}
            />
          </>
        )}
      </section>
    </div>
  );
}

function BackLink() {
  return (
    <Link href="/contacts" className="text-sm text-gray-600 hover:text-gray-900">
      ← Contacts
    </Link>
  );
}

function BalanceCard({ name, balance }: { name: string; balance: number }) {
  if (balance === 0) {
    return (
      <div className="rounded-lg border bg-white p-4">
        <p className="text-sm text-gray-500">Balance</p>
        <p className="mt-1 text-2xl font-semibold text-gray-700">All settled up</p>
      </div>
    );
  }

  const owesYou = balance > 0;

  return (
    <div className="rounded-lg border bg-white p-4">
      <p className="text-sm text-gray-500">{owesYou ? `${name} owes you` : `You owe ${name}`}</p>
      <p className={`mt-1 text-2xl font-semibold ${owesYou ? "text-emerald-700" : "text-rose-700"}`}>
        {formatMoney(Math.abs(balance))}
      </p>
    </div>
  );
}