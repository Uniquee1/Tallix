"use client";

import { ApiError } from "@/lib/api-client";

export function ErrorMessage({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  const signedOut = error instanceof ApiError && error.status === 401;

  return (
    <div className="rounded border border-red-300 bg-red-50 p-4 text-red-800">
      <p>{error.message}</p>

      <div className="mt-2 text-sm">
        {signedOut ? (
          <a href="/auth/login" className="underline">
            Sign in again
          </a>
        ) : (
          onRetry && (
            <button type="button" onClick={onRetry} className="underline">
              Try again
            </button>
          )
        )}
      </div>
    </div>
  );
}