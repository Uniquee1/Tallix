import type { SQLiteDatabase } from "expo-sqlite";
import { toCentavos } from "../money";
import type { Contact, LedgerEntry } from "../types";

// UTC text with millisecond precision, so it sorts correctly as plain text.
export function toUtcText(value: string): string {
  // Trim any extra fraction digits before parsing: JavaScript only guarantees three.
  const date = new Date(value.replace(/(\.\d{3})\d+/, "$1"));

  if (Number.isNaN(date.getTime())) {
    throw new Error(`Unreadable date from the server: ${value}`);
  }

  return date.toISOString();
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

export async function upsertContact(db: SQLiteDatabase, c: Contact): Promise<void> {
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

export async function upsertEntry(db: SQLiteDatabase, e: LedgerEntry): Promise<void> {
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

// Removes a contact, but only if nothing local still refers to it.
export async function deleteContactIfUnreferenced(db: SQLiteDatabase, id: string): Promise<void> {
  await db.runAsync(
    `DELETE FROM contacts
     WHERE id = ? AND NOT EXISTS (SELECT 1 FROM ledger_entries WHERE contact_id = ?)`,
    [id, id],
  );
}