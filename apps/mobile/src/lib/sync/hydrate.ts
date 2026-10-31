import { apiGet } from "../api";
import { getDb, wipeDatabase } from "../db/database";
import { getMeta, setMeta } from "../db/meta";
import { deleteContactIfUnreferenced, upsertContact, upsertEntry } from "../db/store";
import type { Contact, LedgerEntry, User } from "../types";

type SnapshotPage<T> = { items: T[]; nextCursor: string | null };
type HydrateResult = { contacts: number; entries: number };

const PAGE_SIZE = 500;

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

async function run(): Promise<HydrateResult> {
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
    // Who this data belongs to, so the Account tab can show it offline.
    await setMeta("owner_user_id", me.id);
    await setMeta("owner_display_name", me.displayName);
    await setMeta("owner_email", me.email);

    for (const c of contacts) {
      await upsertContact(db, c);
    }

    for (const e of ordered) {
      await upsertEntry(db, e);
    }

    // Contacts deleted on the server: remove our synced copy,
    // but only if nothing local refers to it.
    const local = await db.getAllAsync<{ id: string }>(
      "SELECT id FROM contacts WHERE sync_status = 'SYNCED'",
    );

    for (const { id } of local) {
      if (!seenContacts.has(id)) {
        await deleteContactIfUnreferenced(db, id);
      }
    }

    await setMeta("last_snapshot_at", new Date().toISOString());
  });

  return { contacts: contacts.length, entries: entries.length };
}

let inFlight: Promise<HydrateResult> | null = null;

// Launch, pull-to-refresh and the button can overlap. They all share one download,
// because two transactions on one connection would collide.
export function hydrateFromServer(): Promise<HydrateResult> {
  if (!inFlight) {
    inFlight = run().finally(() => {
      inFlight = null;
    });
  }

  return inFlight;
}