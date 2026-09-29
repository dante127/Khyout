import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import RfqCreatePage from '../RfqCreatePage';
import * as rfqsApi from '../../lib/api/rfqs';
import * as catalogApi from '../../lib/api/catalog';
import type { ProductDetail } from '../../lib/api/catalog';
import type { RfqDetail } from '../../lib/api/rfqs';
import { newOutboxItem, outbox } from '../../offline/outbox';
import { useOnline } from '../../offline/useOnline';

vi.mock('../../lib/api/rfqs', () => ({
  createRfq: vi.fn(),
  getMyRfqs: vi.fn(),
  getRfq: vi.fn(),
  getRfqBids: vi.fn(),
  closeRfq: vi.fn(),
  extendRfq: vi.fn(),
  submitQuotation: vi.fn(),
  acceptQuotation: vi.fn(),
  rejectQuotation: vi.fn(),
  withdrawQuotation: vi.fn(),
}));

vi.mock('../../lib/api/catalog', () => ({
  searchProducts: vi.fn(),
  getCategoryTree: vi.fn(),
  getProduct: vi.fn(),
  createProduct: vi.fn(),
  updateProduct: vi.fn(),
  uploadProductImage: vi.fn(),
}));

vi.mock('../../offline/useOnline', () => ({ useOnline: vi.fn() }));

vi.mock('../../offline/outbox', () => ({
  newOutboxItem: vi.fn((method: string, url: string, body?: unknown) => ({
    id: 'queued-1',
    method,
    url,
    body,
    createdAt: '2026-09-29T00:00:00Z',
    attempts: 0,
  })),
  outbox: { enqueue: vi.fn() },
}));

const mockedRfqs = vi.mocked(rfqsApi);
const mockedCatalog = vi.mocked(catalogApi);
const mockedOnline = vi.mocked(useOnline);
const mockedEnqueue = vi.mocked(outbox.enqueue);
const mockedNewOutboxItem = vi.mocked(newOutboxItem);

const productFixture: ProductDetail = {
  id: 'p1',
  title: 'جينز قطني',
  description: null,
  categoryId: 'cat-1',
  categoryName: 'أقمشة قطنية',
  moq: 100,
  unitOfMeasure: 'Meter',
  indicativePrice: 4.5,
  currency: 'USD',
  status: 'Active',
  gsm: 320,
  gsmTolerancePct: 5,
  weaveStructure: 'Twill',
  widthCm: 150,
  colorFamily: 'كحلي',
  weightPerMeterG: 400,
  careNotes: null,
  composition: [{ fiberType: 'Cotton', percentage: 98 }],
  images: [],
  supplierCompanyId: 's1',
  supplierName: 'مصنع الشام',
  supplierCity: 'دمشق',
  createdAt: '2026-09-01T00:00:00Z',
  updatedAt: '2026-09-02T00:00:00Z',
};

const createdRfq: RfqDetail = {
  id: 'new-rfq-1',
  buyerCompanyId: 'b1',
  buyerCompanyName: 'ورشة النور',
  categoryId: 'cat-1',
  categoryName: 'أقمشة قطنية',
  title: 'توريد جينز قطني',
  description: null,
  quantityNeeded: 600,
  unitOfMeasure: 'Meter',
  targetDeliveryDate: '2026-11-15',
  closingDate: '2026-11-01T15:00:00Z',
  status: 'Open',
  acceptedQuotationId: null,
  createdAt: '2026-09-29T00:00:00Z',
  updatedAt: '2026-09-29T00:00:00Z',
};

const tree = [
  {
    id: 'cat-1',
    slug: 'cotton',
    nameAr: 'أقمشة قطنية',
    nameEn: 'Cotton',
    sortOrder: 1,
    children: [
      { id: 'cat-2', slug: 'denim', nameAr: 'جينز', nameEn: 'Denim', sortOrder: 1, children: [] },
    ],
  },
];

function renderCreate(initial = '/rfqs/new?productId=p1') {
  return render(
    <MemoryRouter initialEntries={[initial]}>
      <Routes>
        <Route path="/rfqs/new" element={<RfqCreatePage />} />
        <Route path="/rfqs/:rfqId" element={<div>صفحة الطلب</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('RfqCreatePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedOnline.mockReturnValue(true);
    mockedCatalog.getCategoryTree.mockResolvedValue(tree);
    mockedCatalog.getProduct.mockResolvedValue(productFixture);
  });

  it('prefills from the source product and creates the RFQ', async () => {
    mockedRfqs.createRfq.mockResolvedValue(createdRfq);

    renderCreate();

    expect(await screen.findByText(/مرتبط بالمنتج/)).toBeInTheDocument();
    await screen.findByRole('option', { name: 'أقمشة قطنية' });
    expect((screen.getByLabelText('عنوان الطلب') as HTMLInputElement).value).toBe('توريد جينز قطني');
    expect((screen.getByLabelText('الفئة') as HTMLSelectElement).value).toBe('cat-1');

    fireEvent.change(screen.getByLabelText('الكمية'), { target: { value: '600' } });
    fireEvent.change(screen.getByLabelText('تاريخ التسليم المطلوب'), { target: { value: '2026-11-15' } });
    fireEvent.change(screen.getByLabelText('موعد إغلاق العروض'), { target: { value: '2026-11-01T18:00' } });

    await userEvent.click(screen.getByRole('button', { name: 'نشر الطلب' }));

    expect(mockedRfqs.createRfq).toHaveBeenCalledWith(
      expect.objectContaining({
        categoryId: 'cat-1',
        title: 'توريد جينز قطني',
        quantityNeeded: 600,
        unitOfMeasure: 'Meter',
        targetDeliveryDate: '2026-11-15',
        closingDate: expect.stringMatching(/^2026-11-01T/),
      }),
    );
    expect(await screen.findByText('صفحة الطلب')).toBeInTheDocument();
  });

  it('keeps submit disabled until required fields are filled', async () => {
    renderCreate('/rfqs/new');

    await screen.findByRole('option', { name: 'أقمشة قطنية' });

    expect(screen.getByRole('button', { name: 'نشر الطلب' })).toBeDisabled();
  });

  it('queues the RFQ locally when offline', async () => {
    mockedOnline.mockReturnValue(false);
    renderCreate('/rfqs/new');

    await screen.findByRole('option', { name: 'أقمشة قطنية' });

    await userEvent.type(screen.getByLabelText('عنوان الطلب'), 'قطن شتوي');
    fireEvent.change(screen.getByLabelText('الفئة'), { target: { value: 'cat-1' } });
    fireEvent.change(screen.getByLabelText('الكمية'), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText('تاريخ التسليم المطلوب'), { target: { value: '2026-12-01' } });
    fireEvent.change(screen.getByLabelText('موعد إغلاق العروض'), { target: { value: '2026-11-20T12:00' } });

    await userEvent.click(screen.getByRole('button', { name: 'حفظ الطلب للإرسال لاحقاً' }));

    expect(mockedRfqs.createRfq).not.toHaveBeenCalled();
    expect(mockedNewOutboxItem).toHaveBeenCalledWith(
      'POST',
      '/api/v1/rfqs',
      expect.objectContaining({ title: 'قطن شتوي', quantityNeeded: 100 }),
    );
    expect(mockedEnqueue).toHaveBeenCalledTimes(1);
    expect(await screen.findByText(/تم حفظ الطلب على جهازك/)).toBeInTheDocument();
  });
});
