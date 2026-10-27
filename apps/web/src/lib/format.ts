import type { TransactionType } from "@/types/api";

const money = new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" });

export function formatMoney(amount: number): string {
  return money.format(amount);
}

// A date without a time ("2026-11-15") is a calendar day. We build it from its
// parts so the browser's time zone can never shift it to the previous day.
export function formatCalendarDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);

  return new Intl.DateTimeFormat("en-PH", { dateStyle: "medium" }).format(
    new Date(year, month - 1, day),
  );
}

// An instant in time, shown in the browser's own time zone.
export function formatDateTime(isoInstant: string): string {
  return new Intl.DateTimeFormat("en-PH", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(isoInstant));
}

// Today's calendar date in the browser's time zone, as yyyy-MM-dd.
// Call this from event handlers, effects or query functions. Never while rendering.
export function localToday(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

export const transactionLabels: Record<TransactionType, string> = {
  LENT: "Lent",
  BORROWED: "Borrowed",
  PAYMENT_RECEIVED: "Payment received",
  PAYMENT_MADE: "Payment made",
  ADJUSTMENT: "Adjustment",
};

// A calendar day picked by the user becomes an instant at noon in their own time zone,
// so the day stays the same wherever it is displayed later.
export function localDateToInstant(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);

  return new Date(year, month - 1, day, 12, 0, 0).toISOString();
}