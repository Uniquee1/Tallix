import { getDb } from "./database";

export async function getMeta(key: string): Promise<string | null> {
  const db = await getDb();
  const row = await db.getFirstAsync<{ value: string }>(
    "SELECT value FROM sync_metadata WHERE key = ?",
    [key],
  );

  return row?.value ?? null;
}

export async function setMeta(key: string, value: string): Promise<void> {
  const db = await getDb();

  await db.runAsync(
    `INSERT INTO sync_metadata (key, value) VALUES (?, ?)
     ON CONFLICT (key) DO UPDATE SET value = excluded.value`,
    [key, value],
  );
}

export type LocalStats = {
  contacts: number;
  entries: number;
  pending: number;
  lastSnapshotAt: string | null;
};

export async function getLocalStats(): Promise<LocalStats> {
  const db = await getDb();

  const counts = await db.getFirstAsync<{ contacts: number; entries: number; pending: number }>(`
    SELECT
      (SELECT count(*) FROM contacts) AS contacts,
      (SELECT count(*) FROM ledger_entries) AS entries,
      (SELECT count(*) FROM sync_queue WHERE status != 'SYNCED') AS pending
  `);

  return {
    contacts: counts?.contacts ?? 0,
    entries: counts?.entries ?? 0,
    pending: counts?.pending ?? 0,
    lastSnapshotAt: await getMeta("last_snapshot_at"),
  };
}