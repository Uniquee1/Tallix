import { fromCentavos, toCentavos } from "./money";

describe("toCentavos", () => {
  it.each([
    [0, 0],
    [5000, 500000],
    [5000.5, 500050],
    [0.07, 7],
    [1.15, 115], // 1.15 * 100 is 114.99999999999999 in floating point
    [19.99, 1999],
    [-250.25, -25025],
  ])("%p becomes %p", (amount, expected) => {
    expect(toCentavos(amount)).toBe(expected);
  });

  it("rejects more than two decimal places", () => {
    expect(() => toCentavos(10.123)).toThrow();
  });

  it("rejects values that are not usable numbers", () => {
    expect(() => toCentavos(Number.NaN)).toThrow();
    expect(() => toCentavos(Number.POSITIVE_INFINITY)).toThrow();
  });

  it("tolerates floating-point noise", () => {
    expect(toCentavos(0.1 + 0.2)).toBe(30);
  });
});

describe("fromCentavos", () => {
  it.each([199999, 500050, 7])("%p survives the round trip", (centavos) => {
    expect(toCentavos(fromCentavos(centavos))).toBe(centavos);
  });
});