"use client";

import { Suspense } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { NavLinks, NavLinksFallback } from "@/components/NavLinks";
import { apiGet } from "@/lib/api-client";
import type { User } from "@/types/api";

export function AppHeader() {
  const me = useQuery({ queryKey: ["me"], queryFn: () => apiGet<User>("/me") });

  return (
    <header className="border-b bg-white">
      <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 px-4 py-3">
        <div className="flex items-center gap-6">
          <Link href="/dashboard" className="text-lg font-semibold">
            Tallix
          </Link>

          <Suspense fallback={<NavLinksFallback />}>
            <NavLinks />
          </Suspense>
        </div>

        <div className="flex items-center gap-4 text-sm">
          <span className="text-gray-600">{me.data?.displayName}</span>
          <a href="/auth/logout" className="rounded border px-3 py-1 hover:bg-gray-100">
            Log out
          </a>
        </div>
      </div>
    </header>
  );
}