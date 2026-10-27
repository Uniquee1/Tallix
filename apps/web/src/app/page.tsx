export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-6 p-6">
      <h1 className="text-3xl font-semibold">Tallix</h1>
      <p className="text-gray-600">Keep track of who owes you and who you owe.</p>
      <a
        href="/auth/login?returnTo=/dashboard"
        className="rounded bg-gray-900 px-4 py-2 text-center text-white hover:bg-gray-700"
      >
        Log in
      </a>
    </main>
  );
}