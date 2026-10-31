// Adds days to a calendar date (yyyy-MM-dd) with no time zone involved.
// Stops at the last day the calendar supports, like the server does.
export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  const result = new Date(Date.UTC(year, month - 1, day + days));

  if (result.getUTCFullYear() > 9999) {
    return "9999-12-31";
  }

  return result.toISOString().slice(0, 10);
}