import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ProductDetailPage from '../ProductDetailPage';
import * as catalogApi from '../../lib/api/catalog';
import * as samplesApi from '../../lib/api/samples';
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

vi.mock('../../lib/api/samples', () => ({
  createSampleRequest: vi.fn(),
  listSamples: vi.fn(),
  updateSampleStatus: vi.fn(),
}));

const mocked = vi.mocked(catalogApi);
const mockedSamples = vi.mocked(samplesApi);

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

  it('lets a buyer request a sample from the product page', async () => {
    tokenStore.set({
      accessToken: makeToken({ sub: 'u-2', name: 'مشتر', role: 'Buyer', companyId: 'b1' }),
      refreshToken: 'refresh',
      accessTokenExpiresAt: '',
      refreshTokenExpiresAt: '',
    });
    mockedSamples.createSampleRequest.mockResolvedValue({
      id: 'sample-1',
      buyerCompanyId: 'b1',
      buyerCompanyName: 'ورشة النور',
      supplierCompanyId: 'sup-1',
      supplierCompanyName: 'مصنع الشام',
      productId: 'p1',
      productTitle: 'قطن مبرد 240',
      rfqQuotationId: null,
      status: 'Requested',
      quantity: 5,
      deliveryCity: 'حلب',
      note: null,
      createdAt: '2026-09-29T00:00:00Z',
      updatedAt: '2026-09-29T00:00:00Z',
    });

    renderDetail();
    await screen.findByText('قطن مبرد 240');

    await userEvent.click(screen.getByRole('button', { name: 'طلب عينة' }));
    fireEvent.change(screen.getByLabelText('الكمية'), { target: { value: '5' } });
    fireEvent.change(screen.getByLabelText('مدينة التسليم'), { target: { value: 'حلب' } });
    await userEvent.click(screen.getByRole('button', { name: 'إرسال طلب العينة' }));

    expect(mockedSamples.createSampleRequest).toHaveBeenCalledWith({
      productId: 'p1',
      quantity: 5,
      deliveryCity: 'حلب',
    });
    expect(await screen.findByText(/تم إرسال طلب العينة/)).toBeInTheDocument();
  });
});
