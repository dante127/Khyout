import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CatalogPage from '../CatalogPage';
import * as catalogApi from '../../lib/api/catalog';
import type { ProductSummary } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';

vi.mock('../../lib/api/catalog', () => ({
  searchProducts: vi.fn(),
  getCategoryTree: vi.fn(),
  getProduct: vi.fn(),
  createProduct: vi.fn(),
  updateProduct: vi.fn(),
  uploadProductImage: vi.fn(),
}));

const mocked = vi.mocked(catalogApi);

function productFixture(overrides: Partial<ProductSummary> = {}): ProductSummary {
  return {
    id: 'p1',
    title: 'جينز قطني',
    categoryId: 'c1',
    categoryName: 'أقمشة الجينز',
    moq: 100,
    unitOfMeasure: 'Meter',
    indicativePrice: 4.5,
    currency: 'USD',
    gsm: 320,
    supplierCompanyId: 's1',
    supplierName: 'مصنع الشام',
    supplierCity: 'دمشق',
    createdAt: '2026-09-20T10:00:00Z',
    ...overrides,
  };
}

function renderCatalog() {
  return render(
    <MemoryRouter>
      <CatalogPage />
    </MemoryRouter>,
  );
}

describe('CatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocked.getCategoryTree.mockResolvedValue([]);
  });

  it('renders products from the search result', async () => {
    mocked.searchProducts.mockResolvedValue({
      items: [productFixture(), productFixture({ id: 'p2', title: 'صوف مبطن' })],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 2,
      totalPages: 1,
    });

    renderCatalog();

    expect(await screen.findByText('جينز قطني')).toBeInTheDocument();
    expect(screen.getByText('صوف مبطن')).toBeInTheDocument();
    expect(screen.getByText('2 نتيجة')).toBeInTheDocument();
    expect(screen.queryByText('تحميل المزيد')).toBeNull();
  });

  it('shows the empty state when nothing matches', async () => {
    mocked.searchProducts.mockResolvedValue({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });

    renderCatalog();

    expect(await screen.findByText('لا توجد منتجات مطابقة بعد')).toBeInTheDocument();
  });

  it('shows an error panel with retry when loading fails', async () => {
    mocked.searchProducts.mockRejectedValue(new ApiError('boom', 'تعذّر تحميل المنتجات', 500));

    renderCatalog();

    expect(await screen.findByRole('alert')).toHaveTextContent('تعذّر تحميل المنتجات');
    expect(screen.getByRole('button', { name: 'إعادة المحاولة' })).toBeInTheDocument();
  });

  it('appends the next page when loading more', async () => {
    mocked.searchProducts
      .mockResolvedValueOnce({
        items: [productFixture()],
        pageNumber: 1,
        pageSize: 20,
        totalCount: 2,
        totalPages: 2,
      })
      .mockResolvedValueOnce({
        items: [productFixture({ id: 'p2', title: 'صوف مبطن' })],
        pageNumber: 2,
        pageSize: 20,
        totalCount: 2,
        totalPages: 2,
      });

    renderCatalog();
    await screen.findByText('جينز قطني');

    await userEvent.click(screen.getByRole('button', { name: 'تحميل المزيد' }));

    expect(await screen.findByText('صوف مبطن')).toBeInTheDocument();
    expect(screen.getByText('جينز قطني')).toBeInTheDocument();
  });
});
