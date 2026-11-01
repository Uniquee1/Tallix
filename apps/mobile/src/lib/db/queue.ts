import type { SQLiteDatabase } from "expo-sqlite";
import { newId, nowText } from "../ids";

export type QueuedOperation = {
  operationType: "CREATE" | "UPDATE" | "DELETE" | "REVERSE";
  entityType: "CONTACT" | "LEDGER_ENTRY";
  entityId: string;
  payload: unknown;
};

// Writes down "this must be sent to the server". Call it in the SAME transaction as the
// change itself. The new row's id becomes the clientOperationId that makes retries safe.
// Operations are replayed in insertion order (rowid), never by timestamp, which can tie.
export async function enqueue(db: SQLiteDatabase, op: QueuedOperation): Promise<void> {
  await db.runAsync(
    `INSERT INTO sync_queue (id, operation_type, entity_type, entity_id, payload, created_at)
     VALUES (?, ?, ?, ?, ?, ?)`,
    [newId(), op.operationType, op.entityType, op.entityId, JSON.stringify(op.payload), nowText()],
  );
}