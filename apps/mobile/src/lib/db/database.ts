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

// Deletes the database file. Used on sign-out and when a different account signs in.
export async function wipeDatabase(): Promise<void> {
  // Opening first guarantees there is a file to delete.
  const db = await getDb();

  await db.closeAsync();
  opening = null;

  await SQLite.deleteDatabaseAsync(DATABASE_NAME);
}