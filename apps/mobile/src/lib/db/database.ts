import * as SQLite from "expo-sqlite";
import { migrate } from "./migrations";

const DATABASE_NAME = "tallix.db";

let opening: Promise<SQLite.SQLiteDatabase> | null = null;

async function open(): Promise<SQLite.SQLiteDatabase> {
  const db = await SQLite.openDatabaseAsync(DATABASE_NAME);

  // WAL lets reads continue while a write is happening.
  // Foreign keys are only enforced if asked, once per connection.
  await db.execAsync("PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;");
  await migrate(db);

  return db;
}

// One connection for the whole app, opened and migrated the first time it's needed.
export function getDb(): Promise<SQLite.SQLiteDatabase> {
  if (!opening) {
    opening = open().catch((error) => {
      opening = null; // let the next call try again
      throw error;
    });
  }

  return opening;
}

let transactions: Promise<void> = Promise.resolve();

// Every transaction goes through here, one at a time. Two transactions at once on the same
// connection collide, and any other query issued while one is open silently runs INSIDE it.
// The work must not call inTransaction itself: it would wait for itself forever.
export function inTransaction<T>(work: (db: SQLite.SQLiteDatabase) => Promise<T>): Promise<T> {
  const run = async (): Promise<T> => {
    const db = await getDb();
    let result: T | undefined;

    await db.withTransactionAsync(async () => {
      result = await work(db);
    });

    return result as T;
  };

  const next = transactions.then(run);

  // The next transaction waits for this one, whether it succeeded or failed.
  transactions = next.then(
    () => undefined,
    () => undefined,
  );

  return next;
}

// Deletes the database file. Used on sign-out and when a different account signs in.
export async function wipeDatabase(): Promise<void> {
  // Opening first guarantees there is a file to delete.
  const db = await getDb();

  await db.closeAsync();
  opening = null;

  await SQLite.deleteDatabaseAsync(DATABASE_NAME);
}