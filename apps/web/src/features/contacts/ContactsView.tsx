"use client";

import { useState } from "react";
import { ErrorMessage } from "@/components/ErrorMessage";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { ContactForm } from "./ContactForm";
import { useContacts, useCreateContact, useDeleteContact, useUpdateContact } from "./api";
import Link from "next/link";
import { Pagination } from "@/components/Pagination";

type Mode =
  | { kind: "idle" }
  | { kind: "create" }
  | { kind: "edit"; id: string }
  | { kind: "delete"; id: string };

const IDLE: Mode = { kind: "idle" };

export function ContactsView() {
  const [searchInput, setSearchInput] = useState("");
  const [page, setPage] = useState(1);
  const [mode, setMode] = useState<Mode>(IDLE);

  const search = useDebouncedValue(searchInput.trim(), 300);
  const contacts = useContacts(search, page);

  const create = useCreateContact();
  const update = useUpdateContact();
  const remove = useDeleteContact();

  // Opening anything (or closing it) clears the previous attempt's error message.
  function openMode(next: Mode) {
    create.reset();
    update.reset();
    remove.reset();
    setMode(next);
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Contacts</h1>
        <button
          type="button"
          onClick={() => openMode({ kind: "create" })}
          className="rounded bg-gray-900 px-4 py-2 text-white hover:bg-gray-700"
        >
          Add contact
        </button>
      </div>

      <input
        type="search"
        aria-label="Search contacts"
        placeholder="Search by name, email or phone"
        value={searchInput}
        onChange={(event) => {
          setSearchInput(event.target.value);
          setPage(1);
        }}
        className="w-full rounded border bg-white px-3 py-2"
      />

      {mode.kind === "create" && (
        <ContactForm
          submitLabel="Add contact"
          pending={create.isPending}
          error={create.error}
          onSubmit={(values) => create.mutate(values, { onSuccess: () => setMode(IDLE) })}
          onCancel={() => openMode(IDLE)}
        />
      )}

      {contacts.status === "pending" && <p className="text-gray-500">Loading contacts…</p>}

      {contacts.status === "error" && (
        <ErrorMessage
          error={contacts.error}
          onRetry={() => {
            void contacts.refetch();
          }}
        />
      )}

      {contacts.status === "success" && (
        <>
          {contacts.data.totalCount === 0 ? (
            <div className="rounded-lg border border-dashed bg-white p-8 text-center text-gray-600">
              {search === "" ? "No contacts yet. Add your first one." : `No contacts match “${search}”.`}
            </div>
          ) : (
            <ul
              className={`divide-y rounded-lg border bg-white ${
                contacts.isPlaceholderData ? "opacity-60" : ""
              }`}
            >
              {contacts.data.items.map((contact) => (
                <li key={contact.id} className="p-4">
                  {mode.kind === "edit" && mode.id === contact.id ? (
                    <ContactForm
                      initial={contact}
                      submitLabel="Save changes"
                      pending={update.isPending}
                      error={update.error}
                      onSubmit={(values) =>
                        update.mutate(
                          { id: contact.id, input: values },
                          { onSuccess: () => setMode(IDLE) },
                        )
                      }
                      onCancel={() => openMode(IDLE)}
                    />
                  ) : (
                    <>
                      <div className="flex items-start justify-between gap-4">
                        <div className="min-w-0">
                            <Link href={`/contacts/${contact.id}`} className="font-medium hover:underline">
                                {contact.name}
                            </Link>
                          {(contact.phone || contact.email) && (
                            <p className="text-sm text-gray-600">
                              {[contact.phone, contact.email].filter(Boolean).join(" · ")}
                            </p>
                          )}
                          {contact.notes && (
                            <p className="line-clamp-2 text-sm text-gray-500">{contact.notes}</p>
                          )}
                        </div>

                        <div className="flex shrink-0 gap-3 text-sm">
                          <button
                            type="button"
                            onClick={() => openMode({ kind: "edit", id: contact.id })}
                            className="underline"
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            onClick={() => openMode({ kind: "delete", id: contact.id })}
                            className="text-red-700 underline"
                          >
                            Delete
                          </button>
                        </div>
                      </div>

                      {mode.kind === "delete" && mode.id === contact.id && (
                        <div className="mt-3 rounded bg-amber-50 p-3 text-sm">
                          <p>Delete {contact.name}? This can’t be undone.</p>
                          {remove.error && <p className="mt-1 text-red-700">{remove.error.message}</p>}

                          <div className="mt-2 flex gap-3">
                            <button
                              type="button"
                              disabled={remove.isPending}
                              onClick={() =>
                                remove.mutate(contact.id, {
                                  onSuccess: () => {
                                    // Deleting the last contact on a page would leave an empty page.
                                    if (contacts.data.items.length === 1 && page > 1) {
                                      setPage(page - 1);
                                    }
                                    setMode(IDLE);
                                  },
                                })
                              }
                              className="rounded bg-red-600 px-3 py-1 text-white disabled:opacity-50"
                            >
                              {remove.isPending ? "Deleting…" : "Delete"}
                            </button>
                            <button
                              type="button"
                              onClick={() => openMode(IDLE)}
                              className="rounded border bg-white px-3 py-1"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}

          <Pagination
            page={contacts.data.page}
            totalPages={Math.max(1, Math.ceil(contacts.data.totalCount / contacts.data.pageSize))}
            onChange={setPage}
          />
        </>
      )}
    </div>
  );
}