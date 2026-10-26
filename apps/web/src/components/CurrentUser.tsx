"use client";

import { useQuery } from "@tanstack/react-query";
import { ApiError, apiGet } from "@/lib/api-client";
import type { User } from "@/types/api";

export function CurrentUser() {
  const query = useQuery({
    queryKey: ["me"],
    queryFn: () => apiGet<User>("/me"),
  });

  if (query.status === "pending") {
    return <p className="text-gray-500">Loading…</p>;
  }

  if (query.status === "error") {
    const error = query.error;

    return (
      <div className="rounded border border-red-300 bg-red-50 p-3 text-red-800">
        <p>{error.message}</p>
        {error instanceof ApiError && error.status === 401 && (
          <a href="/auth/login" className="underline">
            Sign in again
          </a>
        )}
      </div>
    );
  }

  return (
    <div className="rounded border bg-white p-4">
      <p className="text-sm text-gray-500">Signed in as</p>
      <p className="text-lg font-medium">{query.data.displayName}</p>
      <p className="text-gray-600">{query.data.email}</p>
    </div>
  );
}