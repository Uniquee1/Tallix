import { addDays } from "./calendar";

describe("addDays", () => {
  it.each([
    ["2026-10-25", 7, "2026-11-01"],
    ["2026-12-30", 5, "2027-01-04"],
    ["2028-02-28", 1, "2028-02-29"], // a leap year
    ["2026-02-28", 1, "2026-03-01"], // not a leap year
    ["9999-12-31", 7, "9999-12-31"], // stops at the end of the calendar
  ])("%s plus %i days is %s", (start, days, expected) => {
    expect(addDays(start, days)).toBe(expected);
  });
});