// Money is stored as whole centavos (₱5,000.50 is 500050), so the phone never
// needs floating-point arithmetic on amounts. Same reason the server uses decimal.

// Converts a decimal amount from the server to centavos.
export function toCentavos(amount: number): number {
  const centavos = Math.round(amount * 100);

  // The server only allows two decimal places. If this fails, we misread the data,
  // and stopping loudly is better than storing a wrong amount.
  if (!Number.isSafeInteger(centavos) || Math.abs(amount * 100 - centavos) > 1e-6) {
    throw new Error(`Not a whole number of centavos: ${amount}`);
  }

  return centavos;
}

// For display only. Never add or compare these results.
export function fromCentavos(centavos: number): number {
  return centavos / 100;
}