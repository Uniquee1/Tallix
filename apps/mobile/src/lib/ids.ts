import * as Crypto from "expo-crypto";

// A new random id (lowercase, v4), made on the phone, with no server involved.
// Lowercase matters: the ledger rules compare ids as text.
export const newId = (): string => Crypto.randomUUID();

// Now, as UTC text with millisecond precision: the same form the database stores.
export const nowText = (): string => new Date().toISOString();