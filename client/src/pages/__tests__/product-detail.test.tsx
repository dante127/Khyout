import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ProductDetailPage from '../ProductDetailPage';
import * as catalogApi from '../../lib/api/catalog';
import type { ProductDetail } from '../../lib/api/catalog';
import { tokenStore } from '../../lib/storage';

vi.mock('../../lib/api/catalog', () => ({
  searchProducts: vi.fn(),
  getCategoryTree: vi.fn(),
  getProduct: vi.fn(),
  createProduct: vi.fn(),
  updateProduct: vi.fn(),
  uploadProductImage: vi.fn(),
}));

const mocked = vi.mocked(catalogApi);

function makeToken(payload: Record<string, unknown>): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  const encoded = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${encoded}.signature`;
}

const detailFixture: ProductDetail = {
  id: 'p1',
  title: 'قطن مبرد 240',
  description: null,
  categoryId: 'c1',
  categoryName: 'أقمشة قطنية',
  moq: 200,
  unitOfMeasure: 'Meter',
  indicativePrice: 3.2,
  currency: 'USD',
  status: 'Active',
  gsm: 240,
  gsmTolerancePct: 5,
  weaveStructure: 'Twill',
  widthCm: 150,
  colorFamily: 'كحلي',
  weightPerMeterG: 360,
  careNotes: null,
  composition: [
    { fiberType: 'Cotton', percentage: 98 },
    { fiberType: 'Lycra', percentage: 2 },
  ],
  images: [],
  supplierCompanyId: 'sup-1',
  supplierName: 'مصنع الشام',
  supplierCity: 'دمشق',
  createdAt: '2026-09-01T00:00:00Z',
  updatedAt: '2026-09-02T00:00:00Z',
};

function renderDetail() {
  return render(
    <MemoryRouter initialEntries={['/catalog/p1']}>
      <Routes>
        <Route path="/catalog/:productId" element={<ProductDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ProductDetailPage', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
    mocked.getProduct.mockResolvedValue(detailFixture);
  });

  it('renders technical specs and composition', async () => {
    renderDetail();

    expect(await screen.findByText('قطن مبرد 240')).toBeInTheDocument();
    expect(screen.getByText('240 GSM ±5%')).toBeInTheDocument();
    expect(screen.getByText('مبرد')).toBeInTheDocument();
    expect(screen.getByText('قطن')).toBeInTheDocument();
    expect(screen.getByText('مصنع الشام')).toBeInTheDocument();
    expect(screen.queryByText('إضافة صورة')).toBeNull();
  });

  it('shows the upload control for the owning supplier', async () => {
    tokenStore.set({
      accessToken: makeToken({ sub: 'u-1', name: 'مورد', role: 'Supplier', companyId: 'sup-1' }),
      refreshToken: 'refresh',
      accessTokenExpiresAt: '',
      refreshTokenExpiresAt: '',
    });

    renderDetail();

    expect(await screen.findByText('إضافة صورة')).toBeInTheDocument();
    expect(screen.getByText('صور المنتج')).toBeInTheDocument();
  });
});
