import 'fake-indexeddb/auto';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushOutbox } from '../flush';
import { newOutboxItem, outbox } from '../outbox';

async function clearAll(): Promise<void> {
  const items = await outbox.all();
  await Promise.all(items.map((item) => outbox.remove(item.id)));
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

const okEnvelope = (data: unknown) => ({ success: true, data, error: null });
const errEnvelope = (code: string, message: string) => ({
  success: false,
  data: null,
  error: { code, message },
});

describe('flushOutbox', () => {
  beforeEach(clearAll);

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('replays queued items in order and removes them on success', async () => {
    await outbox.enqueue({
      ...newOutboxItem('POST', '/api/v1/rfqs', { title: 'قطن شتوي' }),
      createdAt: '2026-09-29T10:00:00Z',
    });
    await outbox.enqueue({
      ...newOutboxItem('PATCH', '/api/v1/samples/s-1/status', { status: 'Approved' }),
      createdAt: '2026-09-29T10:01:00Z',
    });

    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(okEnvelope(null))),
    );
    vi.stubGlobal('fetch', fetchMock);

    const result = await flushOutbox();

    expect(result).toEqual({ sent: 2, dropped: 0, failed: 0 });
    expect(await outbox.count()).toBe(0);
    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/v1/rfqs');
    expect(String(fetchMock.mock.calls[1][0])).toBe('/api/v1/samples/s-1/status');
    expect(fetchMock.mock.calls[1][1]?.method).toBe('PATCH');
  });

  it('keeps items and stops when the network is still down', async () => {
    await outbox.enqueue(newOutboxItem('POST', '/api/v1/rfqs', { title: 'x' }));

    const fetchMock = vi.fn(() => Promise.reject(new TypeError('failed to fetch')));
    vi.stubGlobal('fetch', fetchMock);

    const result = await flushOutbox();

    expect(result).toEqual({ sent: 0, dropped: 0, failed: 1 });
    const items = await outbox.all();
    expect(items).toHaveLength(1);
    expect(items[0].attempts).toBe(1);
    expect(items[0].lastError).toBe('offline');
  });

  it('drops items permanently rejected by the server', async () => {
    await outbox.enqueue(newOutboxItem('POST', '/api/v1/rfqs', { title: 'bad' }));

    const fetchMock = vi.fn(() =>
      Promise.resolve(jsonResponse(errEnvelope('validation_error', 'invalid payload'), 400)),
    );
    vi.stubGlobal('fetch', fetchMock);

    const result = await flushOutbox();

    expect(result).toEqual({ sent: 0, dropped: 1, failed: 0 });
    expect(await outbox.count()).toBe(0);
  });
});
