import { afterEach, describe, expect, it, vi } from 'vitest';
import { createSampleRequest, listSamples, updateSampleStatus } from '../samples';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function ok(data: unknown) {
  return { success: true, data, error: null };
}

describe('samples api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('createSampleRequest posts the payload', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ id: 's-1' }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await createSampleRequest({ productId: 'p1', quantity: 5, deliveryCity: 'حلب', note: null });

    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe('/api/v1/samples');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual({
      productId: 'p1',
      quantity: 5,
      deliveryCity: 'حلب',
      note: null,
    });
  });

  it('listSamples passes status and paging filters', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(
        jsonResponse(ok({ items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
      ),
    );
    vi.stubGlobal('fetch', fetchMock);

    await listSamples({ status: 'Requested', pageNumber: 1, pageSize: 50 });

    const url = new URL(String(fetchMock.mock.calls[0][0]), 'http://localhost');
    expect(url.pathname).toBe('/api/v1/samples');
    expect(url.searchParams.get('status')).toBe('Requested');
    expect(url.searchParams.get('pageSize')).toBe('50');
  });

  it('updateSampleStatus patches the status path', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ id: 's-1', status: 'Shipped' }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    const updated = await updateSampleStatus('s-1', 'Shipped');

    expect(updated.status).toBe('Shipped');
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe('/api/v1/samples/s-1/status');
    expect(init?.method).toBe('PATCH');
    expect(JSON.parse(String(init?.body))).toEqual({ status: 'Shipped' });
  });
});
