import { afterEach, describe, expect, it, vi } from 'vitest';
import { closeRfq, createRfq, getMyRfqs, submitQuotation } from '../rfqs';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function ok(data: unknown) {
  return { success: true, data, error: null };
}

describe('rfqs api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('createRfq posts the full command payload', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ id: 'rfq-1' }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    const input = {
      categoryId: 'cat-1',
      title: 'قماش جينز ثقيل',
      quantityNeeded: 500,
      unitOfMeasure: 'Meter' as const,
      targetDeliveryDate: '2026-10-20',
      closingDate: '2026-10-10T12:00:00Z',
    };
    await createRfq(input);

    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe('/api/v1/rfqs');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual(input);
  });

  it('getMyRfqs passes paging and status filters', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ items: [], pageNumber: 2, pageSize: 10, totalCount: 0, totalPages: 0 }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await getMyRfqs({ status: 'Open', pageNumber: 2, pageSize: 10 });

    const url = new URL(String(fetchMock.mock.calls[0][0]), 'http://localhost');
    expect(url.pathname).toBe('/api/v1/rfqs/mine');
    expect(url.searchParams.get('status')).toBe('Open');
    expect(url.searchParams.get('pageNumber')).toBe('2');
    expect(url.searchParams.get('pageSize')).toBe('10');
  });

  it('submitQuotation targets the rfq quotations path with the bid body', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ quotationId: 'q-1', status: 'Submitted' }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await submitQuotation('rfq-1', {
      unitPrice: 3.5,
      currency: 'USD',
      validUntil: '2026-10-25T00:00:00Z',
      leadTimeDays: 14,
      note: 'متوفر للشحن الفوري',
    });

    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe('/api/v1/rfqs/rfq-1/quotations');
    expect(JSON.parse(String(init?.body))).toEqual({
      unitPrice: 3.5,
      currency: 'USD',
      validUntil: '2026-10-25T00:00:00Z',
      leadTimeDays: 14,
      note: 'متوفر للشحن الفوري',
    });
  });

  it('closeRfq resolves null on the empty success envelope', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse({ success: true, data: null, error: null })),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(closeRfq('rfq-1')).resolves.toBeNull();
    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/v1/rfqs/rfq-1/close');
    expect(fetchMock.mock.calls[0][1]?.method).toBe('POST');
  });
});
