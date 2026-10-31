import { apiGet } from "../api";
import { getDb } from "../db/database";
import { getMeta } from "../db/meta";
import { balanceCentavos as sumBalance, withRunningBalance } from "../ledger/balance";
import { dashboardSummary } from "../ledger/dashboard";
import { compareByTime, signedCentavos, type LedgerRow } from "../ledger/entry";
import type { OutstandingDebt } from "../ledger/outstanding";
import { fromCentavos } from "../money";
import { NotFoundError } from "../notFound";
import type {
  Contact,
  ContactLedger,
  Dashboard,
  DashboardDueGroup,
  TransactionType,
  User,
} from "../types";
import type {
  ContactRepository,
  DashboardRepository,
  LedgerRepository,
  UserRepository,
} from "./contracts";

const MAX_PAGE_SIZE = 100;
const MAX_DUE_ITEMS = 5;
const RECENT_COUNT = 10;

type ContactRow = {
  id: string;
  name: string;
  phone: string | null;
  email: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string;
};

type EntryRow = {
  id: string;
  contact_id: string;
  type: TransactionType;
  amount_centavos: number;
  description: string | null;
  occurred_at: string;
  due_date: string | null;
  created_at: string;
  reverses_entry_id: string | null;
};

const CONTACT_COLUMNS = "id, name, phone, email, notes, created_at, updated_at";
const ENTRY_COLUMNS =
  "id, contact_id, type, amount_centavos, description, occurred_at, due_date, created_at, reverses_entry_id";

const toContact = (r: ContactRow): Contact => ({
  id: r.id,
  name: r.name,
  phone: r.phone,
  email: r.email,
  notes: r.notes,
  createdAt: r.created_at,
  updatedAt: r.updated_at,
});

const toLedgerRow = (r: EntryRow): LedgerRow => ({
  id: r.id,
  contactId: r.contact_id,
  type: r.type,
  amountCentavos: r.amount_centavos,
  occurredAt: r.occurred_at,
  createdAt: r.created_at,
  dueDate: r.due_date,
  reversesEntryId: r.reverses_entry_id,
});

// In a LIKE pattern, % and _ are wildcards and \ is the escape character.
// Same protection as the server's search.
const escapeLike = (value: string) =>
  value.replace(/\\/g, "\\\\").replace(/%/g, "\\%").replace(/_/g, "\\_");

const clampSize = (size: number) => Math.min(Math.max(size, 1), MAX_PAGE_SIZE);

// ---------- Account ----------

export const localUsers: UserRepository = {
  // Saved when the data was downloaded, so the Account tab works with no network.
  me: async (): Promise<User> => {
    const [id, displayName, email] = await Promise.all([
      getMeta("owner_user_id"),
      getMeta("owner_display_name"),
      getMeta("owner_email"),
    ]);

    if (id && displayName && email) {
      return { id, displayName, email };
    }

    return apiGet<User>("/me");
  },
};

// ---------- Contacts ----------

export const localContactReads: Pick<ContactRepository, "list" | "get"> = {
  list: async ({ search, page, pageSize }) => {
    const db = await getDb();
    const size = clampSize(pageSize);
    const pageNumber = Math.max(page, 1);

    const pattern = `%${escapeLike(search)}%`;
    const where =
      search === ""
        ? ""
        : `WHERE name LIKE ? ESCAPE '\\' OR email LIKE ? ESCAPE '\\' OR phone LIKE ? ESCAPE '\\'`;
    const args = search === "" ? [] : [pattern, pattern, pattern];

    const total = await db.getFirstAsync<{ n: number }>(
      `SELECT count(*) AS n FROM contacts ${where}`,
      args,
    );

    const rows = await db.getAllAsync<ContactRow>(
      `SELECT ${CONTACT_COLUMNS} FROM contacts ${where}
       ORDER BY name COLLATE NOCASE, id LIMIT ? OFFSET ?`,
      [...args, size, (pageNumber - 1) * size],
    );

    return {
      items: rows.map(toContact),
      page: pageNumber,
      pageSize: size,
      totalCount: total?.n ?? 0,
    };
  },

  get: async (id) => {
    const db = await getDb();
    const row = await db.getFirstAsync<ContactRow>(
      `SELECT ${CONTACT_COLUMNS} FROM contacts WHERE id = ?`,
      [id],
    );

    if (!row) {
      throw new NotFoundError("Contact not found.");
    }

    return toContact(row);
  },
};

// ---------- A contact's history ----------

export const localLedgerReads: Pick<LedgerRepository, "forContact"> = {
  forContact: async ({ contactId, page, pageSize }): Promise<ContactLedger> => {
    const db = await getDb();

    const exists = await db.getFirstAsync<{ id: string }>(
      "SELECT id FROM contacts WHERE id = ?",
      [contactId],
    );

    if (!exists) {
      throw new NotFoundError("Contact not found.");
    }

    const rows = await db.getAllAsync<EntryRow>(
      `SELECT ${ENTRY_COLUMNS} FROM ledger_entries WHERE contact_id = ?`,
      [contactId],
    );

    const descriptions = new Map(rows.map((r) => [r.id, r.description]));
    const entries = rows.map(toLedgerRow);

    // The running balance comes from the whole history, so a later page is still correct.
    const running = withRunningBalance(entries);

    const reversedBy = new Map<string, string>();
    for (const entry of entries) {
      if (entry.reversesEntryId !== null) {
        reversedBy.set(entry.reversesEntryId, entry.id);
      }
    }

    const size = clampSize(pageSize);
    const pageNumber = Math.max(page, 1);

    // Newest first, like the server's history.
    const items = [...running]
      .reverse()
      .slice((pageNumber - 1) * size, pageNumber * size)
      .map(({ entry, balanceCentavos: after }) => ({
        id: entry.id,
        type: entry.type,
        amount: fromCentavos(entry.amountCentavos),
        currency: "PHP" as const,
        signedAmount: fromCentavos(signedCentavos(entry)),
        runningBalance: fromCentavos(after),
        description: descriptions.get(entry.id) ?? null,
        occurredAt: entry.occurredAt,
        dueDate: entry.dueDate,
        createdAt: entry.createdAt,
        reversesEntryId: entry.reversesEntryId,
        reversedByEntryId: reversedBy.get(entry.id) ?? null,
      }));

    return {
      contactId,
      balance: fromCentavos(sumBalance(entries)),
      currency: "PHP",
      history: { items, page: pageNumber, pageSize: size, totalCount: running.length },
    };
  },
};

// ---------- The dashboard ----------

function toGroup(debts: OutstandingDebt[], nameOf: (id: string) => string): DashboardDueGroup {
  const total = (direction: OutstandingDebt["direction"]) =>
    debts
      .filter((d) => d.direction === direction)
      .reduce((sum, d) => sum + d.outstandingCentavos, 0);

  return {
    count: debts.length,
    owedToMeTotal: fromCentavos(total("OWED_TO_ME")),
    iOweTotal: fromCentavos(total("I_OWE")),
    items: debts.slice(0, MAX_DUE_ITEMS).map((d) => ({
      entryId: d.entry.id,
      contactId: d.entry.contactId,
      contactName: nameOf(d.entry.contactId),
      direction: d.direction,
      outstanding: fromCentavos(d.outstandingCentavos),
      dueDate: d.entry.dueDate ?? "",
    })),
  };
}

export const localDashboard: DashboardRepository = {
  get: async (asOf): Promise<Dashboard> => {
    const db = await getDb();

    const contacts = await db.getAllAsync<{ id: string; name: string }>(
      "SELECT id, name FROM contacts",
    );
    const names = new Map(contacts.map((c) => [c.id, c.name]));
    const nameOf = (id: string) => names.get(id) ?? "Unknown contact";

    const entries = (
      await db.getAllAsync<EntryRow>(`SELECT ${ENTRY_COLUMNS} FROM ledger_entries`)
    ).map(toLedgerRow);

    const summary = dashboardSummary(entries, asOf);

    // Newest first: the same order as the history, reversed.
    const recent = [...entries]
      .sort((a, b) => compareByTime(b, a))
      .slice(0, RECENT_COUNT)
      .map((e) => ({
        entryId: e.id,
        contactId: e.contactId,
        contactName: nameOf(e.contactId),
        type: e.type,
        amount: fromCentavos(e.amountCentavos),
        signedAmount: fromCentavos(signedCentavos(e)),
        occurredAt: e.occurredAt,
        reversesEntryId: e.reversesEntryId,
      }));

    return {
      asOf,
      currency: "PHP",
      totalOwedToMe: fromCentavos(summary.totalOwedToMeCentavos),
      totalIOwe: fromCentavos(summary.totalIOweCentavos),
      netBalance: fromCentavos(summary.totalOwedToMeCentavos - summary.totalIOweCentavos),
      activeContactCount: summary.activeContactCount,
      overdue: toGroup(summary.overdue, nameOf),
      upcoming: toGroup(summary.upcoming, nameOf),
      recentTransactions: recent,
    };
  },
};