import { ValidationError } from "../ruleErrors";
import type { ContactInput, TransactionType } from "../types";

export const MAX_DESCRIPTION_LENGTH = 500;
export const CONTACT_LIMITS = { name: 200, phone: 50, email: 320, notes: 2000 } as const;

// LedgerEntry.Create's rules about amount and description. Returns the description as the
// server will store it (trimmed), or throws with the server's message.
export function normalizeEntry(input: {
  type: TransactionType;
  amountCentavos: number;
  description: string | null;
}): { description: string | null } {
  if (input.type === "ADJUSTMENT") {
    if (input.amountCentavos === 0) {
      throw new ValidationError("An adjustment cannot be zero.");
    }
  } else if (input.amountCentavos <= 0) {
    throw new ValidationError("Amount must be greater than zero.");
  }

  const description = input.description === null ? null : input.description.trim();

  if (description !== null && description.length > MAX_DESCRIPTION_LENGTH) {
    throw new ValidationError(`Description cannot exceed ${MAX_DESCRIPTION_LENGTH} characters.`);
  }

  return { description };
}

// Optional text: trimmed, and blank becomes null so "no phone" is stored one way only.
function optional(value: string | null, max: number, field: string): string | null {
  const trimmed = value?.trim();

  if (!trimmed) {
    return null;
  }

  if (trimmed.length > max) {
    throw new ValidationError(`${field} cannot exceed ${max} characters.`);
  }

  return trimmed;
}

// Contact.Create / Contact.Update's rules: what the server will store for this input.
export function normalizeContact(input: ContactInput): ContactInput {
  const name = input.name.trim();

  if (name === "") {
    throw new ValidationError("Contact name is required.");
  }

  if (name.length > CONTACT_LIMITS.name) {
    throw new ValidationError(`Contact name cannot exceed ${CONTACT_LIMITS.name} characters.`);
  }

  return {
    name,
    phone: optional(input.phone, CONTACT_LIMITS.phone, "Phone"),
    email: optional(input.email, CONTACT_LIMITS.email, "Email"),
    notes: optional(input.notes, CONTACT_LIMITS.notes, "Notes"),
  };
}