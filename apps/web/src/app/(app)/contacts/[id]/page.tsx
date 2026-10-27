import { Suspense } from "react";
import { ContactLedgerView } from "@/features/ledger/ContactLedgerView";

export const metadata = { title: "Contact · Tallix" };

type Params = Promise<{ id: string }>;

// Awaiting params is the runtime read, so it lives inside the Suspense boundary.
async function ContactContent({ params }: { params: Params }) {
  const { id } = await params;

  return <ContactLedgerView contactId={id} />;
}

export default function ContactPage({ params }: { params: Params }) {
  return (
    <Suspense fallback={<p className="text-gray-500">Loading…</p>}>
      <ContactContent params={params} />
    </Suspense>
  );
}