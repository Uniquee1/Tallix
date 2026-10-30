import type { SQLiteDatabase } from "expo-sqlite";

// The schema, one step at a time. Never edit a step that has shipped:
// add a new one at the end. PRAGMA user_version records how many have run.
export const migrations: Array<(db: SQLiteDatabase) => Promise<void>> = [
  // Version 1: the tables from the requirements (FR-018).
  async (db) => {
    await db.execAsync(`
      CREATE TABLE contacts (
        id          TEXT PRIMARY KEY NOT NULL,
        name        TEXT NOT NULL,
        phone       TEXT,
        email       TEXT,
        notes       TEXT,
        created_at  TEXT NOT NULL,
        updated_at  TEXT NOT NULL,
        sync_status TEXT NOT NULL DEFAULT 'SYNCED'
          CHECK (sync_status IN ('SYNCED', 'PENDING', 'FAILED'))
      );
      CREATE INDEX ix_contacts_name ON contacts (name COLLATE NOCASE);

      CREATE TABLE ledger_entries (
        id                TEXT PRIMARY KEY NOT NULL,
        contact_id        TEXT NOT NULL REFERENCES contacts (id),
        type              TEXT NOT NULL
          CHECK (type IN ('LENT', 'BORROWED', 'PAYMENT_RECEIVED', 'PAYMENT_MADE', 'ADJUSTMENT')),
        amount_centavos   INTEGER NOT NULL,
        currency          TEXT NOT NULL DEFAULT 'PHP',
        description       TEXT,
        occurred_at       TEXT NOT NULL,
        due_date          TEXT,
        created_at        TEXT NOT NULL,
        reverses_entry_id TEXT REFERENCES ledger_entries (id),
        sync_status       TEXT NOT NULL DEFAULT 'SYNCED'
          CHECK (sync_status IN ('SYNCED', 'PENDING', 'FAILED'))
      );
      CREATE INDEX ix_ledger_entries_contact ON ledger_entries (contact_id, occurred_at);

      -- The same rule as on the server: an entry can be reversed once.
      CREATE UNIQUE INDEX ux_ledger_entries_reverses
        ON ledger_entries (reverses_entry_id) WHERE reverses_entry_id IS NOT NULL;

      CREATE TABLE sync_queue (
        id             TEXT PRIMARY KEY NOT NULL, -- will also be the clientOperationId
        operation_type TEXT NOT NULL
          CHECK (operation_type IN ('CREATE', 'UPDATE', 'DELETE', 'REVERSE')),
        entity_type    TEXT NOT NULL CHECK (entity_type IN ('CONTACT', 'LEDGER_ENTRY')),
        entity_id      TEXT NOT NULL,
        payload        TEXT NOT NULL,             -- JSON
        created_at     TEXT NOT NULL,
        attempt_count  INTEGER NOT NULL DEFAULT 0,
        status         TEXT NOT NULL DEFAULT 'PENDING'
          CHECK (status IN ('PENDING', 'SYNCING', 'SYNCED', 'FAILED')),
        last_error     TEXT
      );
      CREATE INDEX ix_sync_queue_status ON sync_queue (status, created_at);

      CREATE TABLE sync_metadata (
        key   TEXT PRIMARY KEY NOT NULL,
        value TEXT NOT NULL
      );
    `);
  },
];

export async function migrate(db: SQLiteDatabase): Promise<void> {
  const row = await db.getFirstAsync<{ user_version: number }>("PRAGMA user_version");
  let version = row?.user_version ?? 0;

  while (version < migrations.length) {
    const step = version;

    // The step and the version bump succeed or fail together.
    await db.withTransactionAsync(async () => {
      await migrations[step](db);
      await db.execAsync(`PRAGMA user_version = ${step + 1}`);
    });

    version = step + 1;
  }
}