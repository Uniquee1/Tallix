"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const links = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/contacts", label: "Contacts" },
];

// Highlights the current page. Reading the path needs a Suspense boundary
// around this component (see AppHeader).
export function NavLinks() {
  const pathname = usePathname();

  return (
    <nav className="flex gap-4 text-sm">
      {links.map(({ href, label }) => {
        const active = pathname === href || pathname.startsWith(`${href}/`);

        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? "page" : undefined}
            className={active ? "font-medium text-gray-900" : "text-gray-600 hover:text-gray-900"}
          >
            {label}
          </Link>
        );
      })}
    </nav>
  );
}

// Shown while the real path is unknown: the same links, with nothing highlighted.
export function NavLinksFallback() {
  return (
    <nav className="flex gap-4 text-sm">
      {links.map(({ href, label }) => (
        <Link key={href} href={href} className="text-gray-600 hover:text-gray-900">
          {label}
        </Link>
      ))}
    </nav>
  );
}