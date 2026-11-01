import type { SQLiteDatabase } from "expo-sqlite";
import { inTransaction } from "../db/database";
import { enqueue } from "../db/queue";
import { toUtcText } from "../db/store";
import { isValidCalendarDate } from "../format";
import { newId, nowText } from "../ids";
import { reversalAmountCentavos, signedCentavos } from "../ledger/entry";
import { normalizeContact, normalizeEntry } from "../ledger/validate";
import { fromCentavos, toCentavos } from "../money";
import { NotFoundError } from "../notFound";
import { ConflictError, ValidationError } from "../ruleErrors";
import type { Contact, LedgerEntry, TransactionType } from "../types";
import type { ContactRepository, LedgerRepository } from "./contracts";

// ---------- Contacts ----------

export const localContactWrites: Pick<ContactRepository, "create" | "update" | "remove"> = {
  create: (input) =>
    inTransaction(async (db): Promise<Contact> => {
      const values = normalizeContact(input);
      const id = newId();
      const now = nowText();

      await db.runAsync(
        `INSERT INTO contacts (id, name, phone, email, notes, created_at, updated_at, sync_status)
         VALUES (?, ?, ?, ?, ?, ?, ?, 'PENDING')`,
        [id, values.name, values.phone, values.email, values.notes, now, now],
      );

      await enqueue(db, {
        operationType: "CREATE",
        entityType: "CONTACT",
        entityId: id,
        payload: { id, ...values, createdAt: now },
      });

      return { id, ...values, createdAt: now, updatedAt: now, syncStatus: "PENDING" };
    }),

  update: (id, input) =>
    inTransaction(async (db): Promise<Contact> => {
      const values = normalizeContact(input);

      const existing = await db.getFirstAsync<{ created_at: string }>(
        "SELECT created_at FROM contacts WHERE id = ?",
        [id],
      );

      if (!existing) {
        throw new NotFoundError("Contact not found.");
      }

      const now = nowText();

      await db.runAsync(
        `UPDATE contacts
         SET name = ?, phone = ?, email = ?, notes = ?, updated_at = ?, sync_status = 'PENDING'
         WHERE id = ?`,
        [values.name, values.phone, values.email, values.notes, now, id],
      );

      await enqueue(db, {
        operationType: "UPDATE",
        entityType: "CONTACT",
        entityId: id,
        payload: { id, ...values, updatedAt: now },
      });

      return { id, ...values, createdAt: existing.created_at, updatedAt: now, syncStatus: "PENDING" };
    }),

  remove: (id) =>
    inTransaction(async (db) => {
      const exists = await db.getFirstAsync<{ id: string }>(
        "SELECT id FROM contacts WHERE id = ?",
        [id],
      );

      if (!exists) {
        throw new NotFoundError("Contact not found.");
      }

      // The server's rule, applied to pending entries too.
      const hasEntries = await db.getFirstAsync<{ one: number }>(
        "SELECT 1 AS one FROM ledger_entries WHERE contact_id = ? LIMIT 1",
        [id],
      );

      if (hasEntries) {
        throw new ConflictError("This contact has transactions and cannot be deleted.");
      }

      // A contact that never reached the server has nothing to delete there: cancel its
      // queued operations instead of queuing a delete.
      const neverSynced = await db.getFirstAsync<{ one: number }>(
        `SELECT 1 AS one FROM sync_queue
         WHERE entity_type = 'CONTACT' AND entity_id = ?
           AND operation_type = 'CREATE' AND status = 'PENDING' LIMIT 1`,
        [id],
      );

      await db.runAsync("DELETE FROM contacts WHERE id = ?", [id]);

      if (neverSynced) {
        await db.runAsync(
          "DELETE FROM sync_queue WHERE entity_type = 'CONTACT' AND entity_id = ?",
          [id],
        );
      } else {
        await enqueue(db, {
          operationType: "DELETE",
          entityType: "CONTACT",
          entityId: id,
          payload: { id },
        });
      }
    }),
};

// ---------- Ledger entries ----------

type NewEntry = {
  id: string;
  contactId: string;
  type: TransactionType;
  amountCentavos: number;
  description: string | null;
  occurredAt: string;
  dueDate: string | null;
  createdAt: string;
  reversesEntryId: string | null;
};

async function insertEntry(db: SQLiteDatabase, e: NewEntry): Promise<void> {
  await db.runAsync(
    `INSERT INTO ledger_entries (
       id, contact_id, type, amount_centavos, currency, description,
       occurred_at, due_date, created_at, reverses_entry_id, sync_status
     )
     VALUES (?, ?, ?, ?, 'PHP', ?, ?, ?, ?, ?, 'PENDING')`,
    [
      e.id,
      e.contactId,
      e.type,
      e.amountCentavos,
      e.description,
      e.occurredAt,
      e.dueDate,
      e.createdAt,
      e.reversesEntryId,
    ],
  );
}

// The shape the screens expect, built from the phone's own integer money.
function toEntryDto(e: NewEntry): LedgerEntry {
  return {
    id: e.id,
    contactId: e.contactId,
    type: e.type,
    amount: fromCentavos(e.amountCentavos),
    currency: "PHP",
    signedAmount: fromCentavos(signedCentavos(e)),
    description: e.description,
    occurredAt: e.occurredAt,
    dueDate: e.dueDate,
    createdAt: e.createdAt,
    reversesEntryId: e.reversesEntryId,
  };
}

function centavosOf(amount: number): number {
  try {
    return toCentavos(amount);
  } catch {
    throw new ValidationError("Amount cannot have more than 2 decimal places.");
  }
}

export const localLedgerWrites: Pick<LedgerRepository, "record" | "reverse"> = {
  record: (contactId, input) =>
    inTransaction(async (db): Promise<LedgerEntry> => {
      const contact = await db.getFirstAsync<{ id: string }>(
        "SELECT id FROM contacts WHERE id = ?",
        [contactId],
      );

      if (!contact) {
        throw new NotFoundError("Contact not found.");
      }

      const amountCentavos = centavosOf(input.amount);
      const { description } = normalizeEntry({
        type: input.type,
        amountCentavos,
        description: input.description,
      });

      if (input.dueDate !== null && !isValidCalendarDate(input.dueDate)) {
        throw new ValidationError("The due date is not valid.");
      }

      const now = nowText();

      const entry: NewEntry = {
        id: newId(),
        contactId,
        type: input.type,
        amountCentavos,
        description,
        occurredAt: input.occurredAt === null ? now : toUtcText(input.occurredAt),
        dueDate: input.dueDate,
        createdAt: now,
        reversesEntryId: null,
      };

      await insertEntry(db, entry);

      await enqueue(db, {
        operationType: "CREATE",
        entityType: "LEDGER_ENTRY",
        entityId: entry.id,
        payload: entry,
      });

      return toEntryDto(entry);
    }),

  reverse: (entryId, description) =>
    inTransaction(async (db): Promise<LedgerEntry> => {
      const original = await db.getFirstAsync<{
        contact_id: string;
        type: TransactionType;
        amount_centavos: number;
        reverses_entry_id: string | null;
      }>(
        "SELECT contact_id, type, amount_centavos, reverses_entry_id FROM ledger_entries WHERE id = ?",
        [entryId],
      );

      if (!original) {
        throw new NotFoundError("Ledger entry not found.");
      }

      if (original.reverses_entry_id !== null) {
        throw new ValidationError("A reversal cannot be reversed. Record a new entry instead.");
      }

      const alreadyReversed = await db.getFirstAsync<{ one: number }>(
        "SELECT 1 AS one FROM ledger_entries WHERE reverses_entry_id = ? LIMIT 1",
        [entryId],
      );

      if (alreadyReversed) {
        throw new ConflictError("This entry has already been reversed.");
      }

      // Even an original that hasn't reached the server yet is reversed in the ordinary way:
      // a new entry that cancels it. Both are sent later, in order. Nothing is ever deleted.
      const amountCentavos = reversalAmountCentavos({
        type: original.type,
        amountCentavos: original.amount_centavos,
      });

      const { description: text } = normalizeEntry({
        type: "ADJUSTMENT",
        amountCentavos,
        description,
      });

      const now = nowText();

      const reversal: NewEntry = {
        id: newId(),
        contactId: original.contact_id,
        type: "ADJUSTMENT",
        amountCentavos,
        description: text,
        occurredAt: now,
        dueDate: null,
        createdAt: now,
        reversesEntryId: entryId,
      };

      await insertEntry(db, reversal);

      await enqueue(db, {
        operationType: "REVERSE",
        entityType: "LEDGER_ENTRY",
        entityId: reversal.id,
        payload: reversal,
      });

      return toEntryDto(reversal);
    }),
};