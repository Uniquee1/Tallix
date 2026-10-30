import { apiGet } from "../api";
import { getDb, wipeDatabase } from "../db/database";
import { getMeta, setMeta } from "../db/meta";
import { toCentavos } from "../money";
import type { Contact, LedgerEntry, User } from "../types";

type SnapshotPage<T> = { items: T[]; nextCursor: string | null };

const PAGE_SIZE = 500;

// UTC text with millisecond precision, so it sorts correctly as plain text.
function toUtcText(value: string): string {
  // Trim any extra fraction digits before parsing: JavaScript only guarantees three.
  const date = new Date(value.replace(/(\.\d{3})\d+/, "$1"));

  if (Number.isNaN(date.getTime())) {
    throw new Error(`Unreadable date from the server: ${value}`);
  }

  return date.toISOString();
}

// Downloads every page of one snapshot endpoint.
async function downloadAll<T>(kind: "contacts" | "entries"): Promise<T[]> {
  const all: T[] = [];
  let cursor: string | null = null;

  do {
    const query: string =
      `limit=${PAGE_SIZE}` + (cursor ? `&after=${encodeURIComponent(cursor)}` : "");
    const page: SnapshotPage<T> = await apiGet<SnapshotPage<T>>(
      `/sync/snapshot/${kind}?${query}`,
    );

    all.push(...page.items);
    cursor = page.nextCursor;
  } while (cursor);

  return all;
}

// A row the server sent replaces the local copy, unless the phone has its own
// change to that row that hasn't been sent yet. Pending local work is never overwritten.
const UPSERT_CONTACT = `
  INSERT INTO contacts (id, name, phone, email, notes, created_at, updated_at, sync_status)
  VALUES (?, ?, ?, ?, ?, ?, ?, 'SYNCED')
  ON CONFLICT (id) DO UPDATE SET
    name = excluded.name,
    phone = excluded.phone,
    email = excluded.email,
    notes = excluded.notes,
    created_at = excluded.created_at,
    updated_at = excluded.updated_at,
    sync_status = 'SYNCED'
  WHERE contacts.sync_status = 'SYNCED'`;

const UPSERT_ENTRY = `
  INSERT INTO ledger_entries (
    id, contact_id, type, amount_centavos, currency, description,
    occurred_at, due_date, created_at, reverses_entry_id, sync_status
  )
  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'SYNCED')
  ON CONFLICT (id) DO UPDATE SET
    contact_id = excluded.contact_id,
    type = excluded.type,
    amount_centavos = excluded.amount_centavos,
    currency = excluded.currency,
    description = excluded.description,
    occurred_at = excluded.occurred_at,
    due_date = excluded.due_date,
    created_at = excluded.created_at,
    reverses_entry_id = excluded.reverses_entry_id,
    sync_status = 'SYNCED'
  WHERE ledger_entries.sync_status = 'SYNCED'`;

export async function hydrateFromServer(): Promise<{ contacts: number; entries: number }> {
  const me = await apiGet<User>("/me");

  // A different account on this phone must never see the previous person's data.
  const owner = await getMeta("owner_user_id");

  if (owner !== null && owner !== me.id) {
    await wipeDatabase();
  }

  // 1. Download everything. No database transaction is open while we wait on the network.
  const contacts = await downloadAll<Contact>("contacts");
  const entries = await downloadAll<LedgerEntry>("entries");

  // The snapshot is ordered by id, which is random. A reversal points at the entry it
  // reverses, and the database requires that entry to exist first. A reversal can't
  // itself be reversed, so "originals first, then reversals" is always enough.
  const ordered = [
    ...entries.filter((e) => e.reversesEntryId === null),
    ...entries.filter((e) => e.reversesEntryId !== null),
  ];

  // 2. Apply it all in one transaction: either everything lands, or nothing does.
  const db = await getDb();
  const seenContacts = new Set(contacts.map((c) => c.id));

  await db.withTransactionAsync(async () => {
    await setMeta("owner_user_id", me.id);

    for (const c of contacts) {
      await db.runAsync(UPSERT_CONTACT, [
        c.id,
        c.name,
        c.phone,
        c.email,
        c.notes,
        toUtcText(c.createdAt),
        toUtcText(c.updatedAt),
      ]);
    }

    for (const e of ordered) {
      await db.runAsync(UPSERT_ENTRY, [
        e.id,
        e.contactId,
        e.type,
        toCentavos(e.amount),
        e.currency,
        e.description,
        toUtcText(e.occurredAt),
        e.dueDate,
        toUtcText(e.createdAt),
        e.reversesEntryId,
      ]);
    }

    // Contacts deleted on the server: remove our synced copy,
    // but only if nothing local refers to it.
    const local = await db.getAllAsync<{ id: string }>(
      "SELECT id FROM contacts WHERE sync_status = 'SYNCED'",
    );

    for (const { id } of local) {
      if (!seenContacts.has(id)) {
        await db.runAsync(
          `DELETE FROM contacts
           WHERE id = ? AND NOT EXISTS (SELECT 1 FROM ledger_entries WHERE contact_id = ?)`,
          [id, id],
        );
      }
    }

    await setMeta("last_snapshot_at", new Date().toISOString());
  });

  return { contacts: contacts.length, entries: entries.length };
}