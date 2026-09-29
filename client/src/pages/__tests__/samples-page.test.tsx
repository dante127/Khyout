import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SamplesPage from '../SamplesPage';
import * as samplesApi from '../../lib/api/samples';
import type { Sample } from '../../lib/api/samples';
import { tokenStore } from '../../lib/storage';

vi.mock('../../lib/api/samples', () => ({
  createSampleRequest: vi.fn(),
  listSamples: vi.fn(),
  updateSampleStatus: vi.fn(),
}));

const mocked = vi.mocked(samplesApi);

function makeToken(payload: Record<string, unknown>): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  const encoded = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${encoded}.signature`;
}

function setToken(role: string) {
  tokenStore.set({
    accessToken: makeToken({ sub: 'u-1', name: 'مستخدم', role, companyId: 'c1' }),
    refreshToken: 'refresh',
    accessTokenExpiresAt: '',
    refreshTokenExpiresAt: '',
  });
}

function sampleFixture(overrides: Partial<Sample> = {}): Sample {
  return {
    id: 'sample-1',
    buyerCompanyId: 'b1',
    buyerCompanyName: 'ورشة النور',
    supplierCompanyId: 's1',
    supplierCompanyName: 'مصنع الشام',
    productId: 'p1',
    productTitle: 'جينز قطني',
    rfqQuotationId: null,
    status: 'Requested',
    quantity: 5,
    deliveryCity: 'حلب',
    note: null,
    createdAt: '2026-09-25T10:00:00Z',
    updatedAt: '2026-09-25T10:00:00Z',
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <SamplesPage />
    </MemoryRouter>,
  );
}

describe('SamplesPage', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it('lists incoming samples for a supplier and advances the status', async () => {
    setToken('Supplier');
    mocked.listSamples.mockResolvedValue({
      items: [sampleFixture()],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
    mocked.updateSampleStatus.mockResolvedValue(sampleFixture({ status: 'Approved' }));

    renderPage();

    expect(await screen.findByText('جينز قطني')).toBeInTheDocument();
    expect(screen.getByText(/من: ورشة النور/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'اعتماد' }));

    expect(mocked.updateSampleStatus).toHaveBeenCalledWith('sample-1', 'Approved');
    expect(await screen.findByRole('button', { name: 'وسم كمشحونة' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'اعتماد' })).toBeNull();
  });

  it('shows a read-only list for buyers', async () => {
    setToken('Buyer');
    mocked.listSamples.mockResolvedValue({
      items: [sampleFixture()],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });

    renderPage();

    expect(await screen.findByText('جينز قطني')).toBeInTheDocument();
    expect(screen.getByText(/إلى: مصنع الشام/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'اعتماد' })).toBeNull();
    expect(screen.getByText(/اطلب العينات من صفحة أي منتج/)).toBeInTheDocument();
  });
});
