import type { TransactionType } from "./types";

const money = new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" });

export function formatMoney(amount: number): string {
  return money.format(amount);
}

// A date without a time ("2026-11-15") is a calendar day. We build it from its
// parts so the phone's time zone can never shift it to the previous day.
export function formatCalendarDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);

  return new Intl.DateTimeFormat("en-PH", {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(year, month - 1, day));
}

// An instant in time, shown in the phone's own time zone.
export function formatDateTime(isoInstant: string): string {
  return new Intl.DateTimeFormat("en-PH", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(isoInstant));
}

// Today's calendar date in the phone's time zone, as yyyy-MM-dd.
// Call it when a request is made, not while rendering.
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

// True for a real calendar day written as yyyy-MM-dd (so 2026-02-30 is rejected).
export function isValidCalendarDate(text: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text);

  if (!match) {
    return false;
  }

  const [, year, month, day] = match.map(Number);
  const date = new Date(year, month - 1, day);

  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
}