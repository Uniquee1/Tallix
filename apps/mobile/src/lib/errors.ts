import { ApiError } from "./api";
import { NotFoundError } from "./notFound";

export function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

// Closing the login browser is not an error worth showing.
export function isUserCancelled(error: unknown): boolean {
  return messageOf(error).includes("user_cancelled");
}

export function isNotFound(error: unknown): boolean {
  return error instanceof NotFoundError || (error instanceof ApiError && error.status === 404);
}