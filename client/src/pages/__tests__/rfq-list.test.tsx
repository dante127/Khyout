import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import RfqsPage from '../RfqsPage';
import * as rfqsApi from '../../lib/api/rfqs';
import type { RfqSummary } from '../../lib/api/rfqs';
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
    accessToken: makeToken({ sub: 'u-1', name: 'مستخدم', role, companyId: 'b1' }),
    refreshToken: 'refresh',
    accessTokenExpiresAt: '',
    refreshTokenExpiresAt: '',
  });
}

function summaryFixture(overrides: Partial<RfqSummary> = {}): RfqSummary {
  return {
    id: 'rfq-1',
    title: 'قطن بوبلين 120',
    categoryId: 'c1',
    categoryName: 'أقمشة قطنية',
    quantityNeeded: 800,
    unitOfMeasure: 'Meter',
    closingDate: '2026-10-20T12:00:00Z',
    status: 'Open',
    bidCount: 2,
    createdAt: '2026-09-20T10:00:00Z',
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <RfqsPage />
    </MemoryRouter>,
  );
}

describe('RfqsPage', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it('lists the buyer RFQs with status chips and a create link', async () => {
    setToken('Buyer');
    mocked.getMyRfqs.mockResolvedValue({
      items: [summaryFixture()],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    renderPage();

    expect(await screen.findByText('قطن بوبلين 120')).toBeInTheDocument();
    expect(screen.getByText('2 عروض')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'مفتوحة' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: '+ طلب جديد' })).toBeInTheDocument();
  });

  it('filters by status when a chip is clicked', async () => {
    setToken('Buyer');
    mocked.getMyRfqs.mockResolvedValue({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });

    renderPage();
    await screen.findByText('لا توجد طلبات بعد — أنشئ طلبك الأول');

    await userEvent.click(screen.getByRole('button', { name: 'مفتوحة' }));

    await waitFor(() =>
      expect(mocked.getMyRfqs).toHaveBeenLastCalledWith({
        status: 'Open',
        pageNumber: 1,
        pageSize: 20,
      }),
    );
  });

  it('shows the supplier Telegram notice instead of a list', () => {
    setToken('Supplier');

    renderPage();

    expect(screen.getByText(/ستصلك إشعارات فورية عبر تيليجرام/)).toBeInTheDocument();
    expect(mocked.getMyRfqs).not.toHaveBeenCalled();
  });
});
