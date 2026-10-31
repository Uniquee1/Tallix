import { getDb } from "../db/database";
import { deleteContactIfUnreferenced, upsertContact, upsertEntry } from "../db/store";
import { apiRepositories } from "./api";
import type { ContactRepository, LedgerRepository } from "./contracts";

// TEMPORARY, until writes work offline (Part D): send the change to the server, then copy
// the server's answer into the local database so the screens (which now read locally)
// show it. A change the server refuses throws before anything reaches the local copy.

export const writeThroughContacts: Pick<ContactRepository, "create" | "update" | "remove"> = {
  create: async (input) => {
    const contact = await apiRepositories.contacts.create(input);
    await upsertContact(await getDb(), contact);
    return contact;
  },

  update: async (id, input) => {
    const contact = await apiRepositories.contacts.update(id, input);
    await upsertContact(await getDb(), contact);
    return contact;
  },

  remove: async (id) => {
    await apiRepositories.contacts.remove(id);
    await deleteContactIfUnreferenced(await getDb(), id);
  },
};

export const writeThroughLedger: Pick<LedgerRepository, "record" | "reverse"> = {
  record: async (contactId, input) => {
    const entry = await apiRepositories.ledger.record(contactId, input);
    await upsertEntry(await getDb(), entry);
    return entry;
  },

  reverse: async (entryId, description) => {
    const reversal = await apiRepositories.ledger.reverse(entryId, description);
    await upsertEntry(await getDb(), reversal);
    return reversal;
  },
};