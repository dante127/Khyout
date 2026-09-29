import 'fake-indexeddb/auto';
import { beforeEach, describe, expect, it } from 'vitest';
import { newOutboxItem, outbox } from '../outbox';

async function clearAll(): Promise<void> {
  const items = await outbox.all();
  await Promise.all(items.map((item) => outbox.remove(item.id)));
}

describe('outbox', () => {
  beforeEach(clearAll);

  it('enqueues and lists items', async () => {
    const item = newOutboxItem('POST', '/api/v1/rfqs', { quantityKg: 500 });
    await outbox.enqueue(item);

    const items = await outbox.all();
    expect(items).toHaveLength(1);
    expect(items[0].url).toBe('/api/v1/rfqs');
    expect(items[0].attempts).toBe(0);
    expect(items[0].body).toEqual({ quantityKg: 500 });
  });

  it('counts queued items', async () => {
    await outbox.enqueue(newOutboxItem('POST', '/api/v1/a'));
    await outbox.enqueue(newOutboxItem('DELETE', '/api/v1/b'));
    expect(await outbox.count()).toBe(2);
  });

  it('removes an item by id', async () => {
    const item = newOutboxItem('PUT', '/api/v1/x');
    await outbox.enqueue(item);
    await outbox.remove(item.id);
    expect(await outbox.count()).toBe(0);
  });

  it('markFailed increments attempts and records the error', async () => {
    const item = newOutboxItem('POST', '/api/v1/y');
    await outbox.enqueue(item);

    await outbox.markFailed(item.id, 'network down');

    const [stored] = await outbox.all();
    expect(stored.attempts).toBe(1);
    expect(stored.lastError).toBe('network down');
  });
});
