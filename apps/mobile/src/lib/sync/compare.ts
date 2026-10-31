import { localToday } from "../format";
import { apiRepositories } from "../repositories/api";
import { localContactReads, localDashboard, localLedgerReads } from "../repositories/local";

// Times differ in spelling between the server and the phone ("+00:00" or "Z", different
// fraction lengths) but mean the same instant, so compare them as numbers.
function normalize(value: unknown): unknown {
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
    return Date.parse(value.replace(/(\.\d{3})\d+/, "$1"));
  }

  if (Array.isArray(value)) {
    return value.map(normalize);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(Object.entries(value).map(([key, v]) => [key, normalize(v)]));
  }

  return value;
}

function firstDifference(server: unknown, phone: unknown, path = "$"): string | null {
  if (Array.isArray(server) && Array.isArray(phone)) {
    if (server.length !== phone.length) {
      return `${path}: ${server.length} items on the server, ${phone.length} on the phone`;
    }

    for (let i = 0; i < server.length; i++) {
      const difference = firstDifference(server[i], phone[i], `${path}[${i}]`);
      if (difference) return difference;
    }

    return null;
  }

  if (server && phone && typeof server === "object" && typeof phone === "object") {
    const a = server as Record<string, unknown>;
    const b = phone as Record<string, unknown>;

    for (const key of new Set([...Object.keys(a), ...Object.keys(b)])) {
      const difference = firstDifference(a[key], b[key], `${path}.${key}`);
      if (difference) return difference;
    }

    return null;
  }

  return Object.is(server, phone)
    ? null
    : `${path}: server ${JSON.stringify(server)}, phone ${JSON.stringify(phone)}`;
}

// Runs the dashboard and every contact's history through BOTH implementations
// (the server's C# and the phone's TypeScript) and reports where they disagree.
export async function compareWithServer(): Promise<string[]> {
  const asOf = localToday();
  const lines: string[] = [];

  const [serverDashboard, phoneDashboard] = await Promise.all([
    apiRepositories.dashboard.get(asOf),
    localDashboard.get(asOf),
  ]);

  const dashboardDifference = firstDifference(
    normalize(serverDashboard),
    normalize(phoneDashboard),
  );
  lines.push(dashboardDifference ? `Dashboard differs at ${dashboardDifference}` : "Dashboard: identical");

  const contacts = await localContactReads.list({ search: "", page: 1, pageSize: 100 });
  let differences = 0;

  for (const contact of contacts.items) {
    const request = { contactId: contact.id, page: 1, pageSize: 100 };
    const [server, phone] = await Promise.all([
      apiRepositories.ledger.forContact(request),
      localLedgerReads.forContact(request),
    ]);

    const difference = firstDifference(normalize(server), normalize(phone));

    if (difference) {
      differences += 1;
      lines.push(`${contact.name}: ${difference}`);
    }
  }

  lines.push(`History: ${contacts.items.length} contacts compared, ${differences} with differences`);

  return lines;
}