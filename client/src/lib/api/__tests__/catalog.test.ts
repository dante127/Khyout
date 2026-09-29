import { afterEach, describe, expect, it, vi } from 'vitest';
import { getCategoryTree, getProduct, searchProducts, uploadProductImage } from '../catalog';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function ok(data: unknown) {
  return { success: true, data, error: null };
}

const emptyPage = { items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

describe('catalog api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('searchProducts serializes filters into the query string', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ ...emptyPage, pageNumber: 2 }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await searchProducts({
      categoryId: 'cat-1',
      gsmMin: 120,
      gsmMax: 300,
      moqMax: 500,
      city: 'حلب',
      sort: 'moqAsc',
      fibers: ['Cotton:90', 'Wool:30'],
      pageNumber: 2,
      pageSize: 20,
    });

    const url = new URL(String(fetchMock.mock.calls[0][0]), 'http://localhost');
    expect(url.pathname).toBe('/api/v1/products');
    expect(url.searchParams.get('categoryId')).toBe('cat-1');
    expect(url.searchParams.get('gsmMin')).toBe('120');
    expect(url.searchParams.get('gsmMax')).toBe('300');
    expect(url.searchParams.get('moqMax')).toBe('500');
    expect(url.searchParams.get('city')).toBe('حلب');
    expect(url.searchParams.get('sort')).toBe('moqAsc');
    expect(url.searchParams.getAll('fibers')).toEqual(['Cotton:90', 'Wool:30']);
    expect(url.searchParams.get('pageNumber')).toBe('2');
    expect(url.searchParams.get('pageSize')).toBe('20');
  });

  it('omits the query string when no filters are provided', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok(emptyPage))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await searchProducts();

    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/v1/products');
  });

  it('targets the product detail and category tree paths', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(jsonResponse(ok({ id: 'p1' }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    await getProduct('p1');
    await getCategoryTree();

    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/v1/products/p1');
    expect(String(fetchMock.mock.calls[1][0])).toBe('/api/v1/categories');
  });

  it('uploadProductImage posts multipart data with the file field', async () => {
    const fetchMock = vi.fn((_input: unknown, _init?: RequestInit) =>
      Promise.resolve(
        jsonResponse(ok({ id: 'img-1', storagePath: 'hash.webp', widthPx: 800, heightPx: 600, sortOrder: 0 })),
      ),
    );
    vi.stubGlobal('fetch', fetchMock);

    const file = new File([new Uint8Array([1, 2, 3])], 'photo.png', { type: 'image/png' });
    const image = await uploadProductImage('p1', file);

    expect(image.storagePath).toBe('hash.webp');
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toBe('/api/v1/products/p1/images');
    expect(init?.method).toBe('POST');
    expect(init?.body).toBeInstanceOf(FormData);
    expect((init?.body as FormData).get('file')).toBe(file);
  });
});
