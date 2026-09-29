import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import RfqDetailPage from '../RfqDetailPage';
import * as rfqsApi from '../../lib/api/rfqs';
import type { RfqDetail, RfqDetailResult } from '../../lib/api/rfqs';
import { tokenStore } from '../../lib/storage';

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

const mocked = vi.mocked(rfqsApi);

function makeToken(payload: Record<string, unknown>): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  const encoded = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${encoded}.signature`;
}

function setToken(role: string) {
  tokenStore.set({
    accessToken: makeToken({ sub: 'u-1', name: 'مستخدم', role, companyId: role === 'Supplier' ? 's1' : 'b1' }),
    refreshToken: 'refresh',
    accessTokenExpiresAt: '',
    refreshTokenExpiresAt: '',
  });
}

const rfqDetailFixture: RfqDetail = {
  id: 'rfq-1',
  buyerCompanyId: 'b1',
  buyerCompanyName: 'ورشة النور',
  categoryId: 'c1',
  categoryName: 'أقمشة قطنية',
  title: 'قطن بوبلين 120',
  description: null,
  quantityNeeded: 800,
  unitOfMeasure: 'Meter',
  targetDeliveryDate: '2026-11-10',
  closingDate: '2026-10-20T12:00:00Z',
  status: 'Open',
  acceptedQuotationId: null,
  createdAt: '2026-09-20T10:00:00Z',
  updatedAt: '2026-09-20T10:00:00Z',
};

const buyerResult: RfqDetailResult = {
  rfq: rfqDetailFixture,
  isBuyerOwner: true,
  canReceiveBids: true,
  bidCount: 1,
  myQuotation: null,
};

const bidFixture = {
  quotationId: 'q-1',
  supplierCompanyId: 's1',
  supplierName: 'مصنع الشام',
  unitPrice: 3.25,
  currency: 'USD',
  validUntil: '2026-10-25T00:00:00Z',
  leadTimeDays: 14,
  note: 'جاهز للشحن',
  status: 'Submitted',
  createdAt: '2026-09-25T10:00:00Z',
};

function renderDetail() {
  return render(
    <MemoryRouter initialEntries={['/rfqs/rfq-1']}>
      <Routes>
        <Route path="/rfqs/:rfqId" element={<RfqDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('RfqDetailPage — buyer owner', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
    mocked.getRfq.mockResolvedValue(buyerResult);
    mocked.getRfqBids.mockResolvedValue({
      items: [bidFixture],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
  });

  it('shows the received bids and accepts one with confirmation', async () => {
    setToken('Buyer');
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    mocked.acceptQuotation.mockResolvedValue(rfqDetailFixture);

    renderDetail();

    expect(await screen.findByText('مصنع الشام')).toBeInTheDocument();
    expect(screen.getByText('3.25 USD')).toBeInTheDocument();
    expect(screen.getByText('جاهز للشحن')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'قبول وترسية' }));

    expect(confirmSpy).toHaveBeenCalled();
    expect(mocked.acceptQuotation).toHaveBeenCalledWith('q-1');
    confirmSpy.mockRestore();
  });

  it('shows the management panel for an open RFQ', async () => {
    setToken('Buyer');

    renderDetail();

    expect(await screen.findByText('إدارة الطلب')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'إغلاق الطلب الآن' })).toBeInTheDocument();
    expect(screen.getByLabelText('تمديد الإغلاق حتى')).toBeInTheDocument();
  });
});

describe('RfqDetailPage — supplier', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it('offers the bid form when the supplier has not bid yet', async () => {
    setToken('Supplier');
    mocked.getRfq.mockResolvedValue({
      ...buyerResult,
      isBuyerOwner: false,
      bidCount: 0,
      myQuotation: null,
    });
    mocked.submitQuotation.mockResolvedValue({
      quotationId: 'q-9',
      unitPrice: 3.5,
      currency: 'USD',
      validUntil: '2026-10-30T00:00:00Z',
      leadTimeDays: 10,
      note: null,
      status: 'Submitted',
    });

    renderDetail();

    expect(await screen.findByText('تقديم عرضك')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('سعر الوحدة'), { target: { value: '3.5' } });
    fireEvent.change(screen.getByLabelText('صالح حتى'), { target: { value: '2026-10-30' } });
    fireEvent.change(screen.getByLabelText('مدة التوريد (يوم)'), { target: { value: '10' } });

    await userEvent.click(screen.getByRole('button', { name: 'تقديم العرض' }));

    expect(mocked.submitQuotation).toHaveBeenCalledWith(
      'rfq-1',
      expect.objectContaining({ unitPrice: 3.5, currency: 'USD', leadTimeDays: 10 }),
    );
    expect(mocked.getRfqBids).not.toHaveBeenCalled();
  });

  it('shows the own quotation with a withdraw action', async () => {
    setToken('Supplier');
    mocked.getRfq.mockResolvedValue({
      ...buyerResult,
      isBuyerOwner: false,
      bidCount: 1,
      myQuotation: {
        quotationId: 'q-2',
        unitPrice: 3,
        currency: 'USD',
        validUntil: '2026-10-28T00:00:00Z',
        leadTimeDays: 7,
        note: null,
        status: 'Submitted',
      },
    });
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    mocked.withdrawQuotation.mockResolvedValue(null);

    renderDetail();

    expect(await screen.findByText('عرضك المقدَّم')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'سحب العرض' }));

    expect(mocked.withdrawQuotation).toHaveBeenCalledWith('q-2');
    confirmSpy.mockRestore();
  });
});
