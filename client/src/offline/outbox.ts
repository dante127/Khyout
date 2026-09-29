import { withStore } from './db';

export type OutboxMethod = 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface OutboxItem {
  id: string;
  method: OutboxMethod;
  url: string;
  body?: unknown;
  createdAt: string;
  attempts: number;
  lastError?: string;
}

function createId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `outbox-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export function newOutboxItem(method: OutboxMethod, url: string, body?: unknown): OutboxItem {
  return {
    id: createId(),
    method,
    url,
    body,
    createdAt: new Date().toISOString(),
    attempts: 0,
  };
}

async function allItems(): Promise<OutboxItem[]> {
  return (await withStore<OutboxItem[]>('readonly', (store) => store.getAll())) ?? [];
}

export const outbox = {
  async enqueue(item: OutboxItem): Promise<void> {
    await withStore('readwrite', (store) => store.put(item));
  },

  all: allItems,

  async count(): Promise<number> {
    return withStore<number>('readonly', (store) => store.count());
  },

  async remove(id: string): Promise<void> {
    await withStore('readwrite', (store) => store.delete(id));
  },

  async markFailed(id: string, error: string): Promise<void> {
    const items = await allItems();
    const item = items.find((candidate) => candidate.id === id);
    if (!item) return;
    item.attempts += 1;
    item.lastError = error;
    await withStore('readwrite', (store) => store.put(item));
  },
};

/**
 * Phase 4b-2 stub: replays queued mutations in order when connectivity
 * returns (with backoff and permanent-failure handling), wired to the
 * `online` event and Workbox background sync. The queue itself above is
 * complete and testable; the flush wiring is intentionally not active yet.
 */
export async function flushOutboxStub(): Promise<number> {
  return 0;
}
