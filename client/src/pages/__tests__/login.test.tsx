import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import LoginPage from '../LoginPage';
import { AuthProvider } from '../../auth/AuthContext';
import { tokenStore } from '../../lib/storage';
import * as authApi from '../../lib/api/auth';

vi.mock('../../lib/api/auth', () => ({
  requestOtp: vi.fn(),
  verifyOtp: vi.fn(),
}));

const mockedApi = vi.mocked(authApi);

function renderLogin() {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>الصفحة الرئيسية</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

describe('LoginPage', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it('requests an OTP and reveals the code step', async () => {
    mockedApi.requestOtp.mockResolvedValue(null);
    renderLogin();

    await userEvent.type(screen.getByLabelText('رقم الهاتف'), '+963900000000');
    await userEvent.click(screen.getByRole('button', { name: 'إرسال رمز التحقق' }));

    expect(mockedApi.requestOtp).toHaveBeenCalledWith('+963900000000');
    expect(await screen.findByLabelText('رمز التحقق')).toBeInTheDocument();
    expect(screen.getByText(/المكوّن من 6 أرقام/)).toBeInTheDocument();
  });

  it('verifies the code, stores the token pair and lands on home', async () => {
    mockedApi.requestOtp.mockResolvedValue(null);
    mockedApi.verifyOtp.mockResolvedValue({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      accessTokenExpiresAt: '2026-01-01T00:10:00Z',
      refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
    });
    renderLogin();

    await userEvent.type(screen.getByLabelText('رقم الهاتف'), '+963900000000');
    await userEvent.click(screen.getByRole('button', { name: 'إرسال رمز التحقق' }));
    await userEvent.type(await screen.findByLabelText('رمز التحقق'), '123456');
    await userEvent.click(screen.getByRole('button', { name: 'تأكيد الرمز' }));

    expect(mockedApi.verifyOtp).toHaveBeenCalledWith('+963900000000', '123456');
    expect(tokenStore.get()?.accessToken).toBe('access-1');
    expect(await screen.findByText('الصفحة الرئيسية')).toBeInTheDocument();
  });

  it('shows the server error message when OTP request fails', async () => {
    const { ApiError } = await import('../../lib/api/client');
    mockedApi.requestOtp.mockRejectedValue(new ApiError('rate_limited', 'محاولات كثيرة — حاول لاحقاً', 429));
    renderLogin();

    await userEvent.type(screen.getByLabelText('رقم الهاتف'), '+963900000000');
    await userEvent.click(screen.getByRole('button', { name: 'إرسال رمز التحقق' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('محاولات كثيرة — حاول لاحقاً');
  });
});
