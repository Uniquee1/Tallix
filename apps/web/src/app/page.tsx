import { Suspense } from "react";
import { auth0 } from "@/lib/auth0";
import { CurrentUser } from "@/components/CurrentUser";

// The part of the page that depends on the visitor's cookie.
async function SessionPanel() {
  const session = await auth0.getSession();

  if (session) {
    return (
      <>
        <CurrentUser />
        <a
          href="/auth/logout"
          className="rounded border bg-white px-4 py-2 text-center hover:bg-gray-100"
        >
          Log out
        </a>
      </>
    );
  }

  return (
    <>
      <p className="text-gray-600">Keep track of who owes you and who you owe.</p>
      <a
        href="/auth/login"
        className="rounded bg-gray-900 px-4 py-2 text-center text-white hover:bg-gray-700"
      >
        Log in
      </a>
    </>
  );
}

export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-6 p-6">
      <h1 className="text-3xl font-semibold">Tallix</h1>

      <Suspense fallback={<p className="text-gray-500">Loading…</p>}>
        <SessionPanel />
      </Suspense>
    </main>
  );
}