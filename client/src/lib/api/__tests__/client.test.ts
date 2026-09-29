import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, NetworkError, _resetRefreshState, parseEnvelope, request } from '../client';
import { tokenStore } from '../../storage';
import type { TokenPair } from '../types';

const oldPair: TokenPair = {
  accessToken: 'old-access',
  refreshToken: 'old-refresh',
  accessTokenExpiresAt: '2026-01-01T00:00:00Z',
  refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
};

const newPair: TokenPair = {
  accessToken: 'new-access',
  refreshToken: 'new-refresh',
  accessTokenExpiresAt: '2026-01-01T00:10:00Z',
  refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function okEnvelope<T>(data: T) {
  return { success: true, data, error: null };
}

function errEnvelope(code: string, message: string) {
  return { success: false, data: null, error: { code, message } };
}

describe('parseEnvelope', () => {
  it('returns data from a success envelope', () => {
    const envelope = parseEnvelope<{ id: number }>({ success: true, data: { id: 7 }, error: null });
    expect(envelope.success).toBe(true);
    expect(envelope.data).toEqual({ id: 7 });
  });

  it('preserves error details from a failure envelope', () => {
    const envelope = parseEnvelope(errEnvelope('validation_error', 'بيانات غير صالحة'));
    expect(envelope.success).toBe(false);
    expect(envelope.error?.code).toBe('validation_error');
    expect(envelope.error?.message).toBe('بيانات غير صالحة');
  });

  it('throws ApiError for malformed payloads', () => {
    expect(() => parseEnvelope(null)).toThrow(ApiError);
    expect(() => parseEnvelope('not-an-envelope')).toThrow(ApiError);
    expect(() => parseEnvelope({ data: {} })).toThrow(ApiError);
  });
});

describe('request', () => {
  beforeEach(() => {
    window.localStorage.clear();
    _resetRefreshState();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns data and attaches the bearer token when stored', async () => {
    tokenStore.set(oldPair);
    const fetchMock = vi.fn(
      (_input: unknown, _init?: RequestInit) => Promise.resolve(jsonResponse(okEnvelope({ ok: 1 }))),
    );
    vi.stubGlobal('fetch', fetchMock);

    const result = await request<{ ok: number }>('/api/v1/thing');

    expect(result).toEqual({ ok: 1 });
    const init = fetchMock.mock.calls[0][1];
    expect((init?.headers as Record<string, string> | undefined)?.Authorization).toBe('Bearer old-access');
  });

  it('throws ApiError carrying the server code and message on failure envelopes', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve(jsonResponse(errEnvelope('rfq_closed', 'انتهت مدة الطلب'), 400)),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(request('/api/v1/thing', { auth: false })).rejects.toMatchObject({
      name: 'ApiError',
      code: 'rfq_closed',
      message: 'انتهت مدة الطلب',
      status: 400,
    });
  });

  it('wraps transport failures in NetworkError', async () => {
    const fetchMock = vi.fn(() => Promise.reject(new TypeError('failed to fetch')));
    vi.stubGlobal('fetch', fetchMock);

    await expect(request('/api/v1/thing')).rejects.toBeInstanceOf(NetworkError);
  });

  it('shares a single refresh round-trip across concurrent 401s', async () => {
    tokenStore.set(oldPair);
    let refreshCalls = 0;
    let initialCalls = 0;
    let releaseRefresh: (value: Response) => void = () => undefined;
    const refreshGate = new Promise<Response>((resolve) => {
      releaseRefresh = resolve;
    });

    const fetchMock = vi.fn((input: unknown, init?: RequestInit): Promise<Response> => {
      const url = String(input);
      if (url.includes('/api/v1/auth/refresh')) {
        refreshCalls += 1;
        return refreshGate;
      }
      initialCalls += 1;
      const headers = new Headers(init?.headers);
      if (headers.get('Authorization') === `Bearer ${newPair.accessToken}`) {
        return Promise.resolve(jsonResponse(okEnvelope({ n: 1 })));
      }
      return Promise.resolve(jsonResponse(errEnvelope('unauthorized', 'expired'), 401));
    });
    vi.stubGlobal('fetch', fetchMock);

    const first = request<{ n: number }>('/api/v1/thing');
    const second = request<{ n: number }>('/api/v1/thing');

    await vi.waitFor(() => {
      expect(initialCalls).toBe(2);
    });
    // Let both requests park on the refresh gate before releasing it.
    await new Promise((resolve) => setTimeout(resolve, 0));
    releaseRefresh(jsonResponse(okEnvelope(newPair)));

    const [r1, r2] = await Promise.all([first, second]);
    expect(r1).toEqual({ n: 1 });
    expect(r2).toEqual({ n: 1 });
    expect(refreshCalls).toBe(1);
    expect(tokenStore.get()?.accessToken).toBe(newPair.accessToken);
  });

  it('clears tokens and surfaces an auth error when refresh fails', async () => {
    tokenStore.set(oldPair);
    const fetchMock = vi.fn((input: unknown): Promise<Response> => {
      const url = String(input);
      if (url.includes('/api/v1/auth/refresh')) {
        return Promise.resolve(jsonResponse(errEnvelope('invalid_refresh_token', 'expired'), 400));
      }
      return Promise.resolve(jsonResponse(errEnvelope('unauthorized', 'expired'), 401));
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(request('/api/v1/thing')).rejects.toMatchObject({
      name: 'ApiError',
      status: 401,
    });
    expect(tokenStore.get()).toBeNull();
  });
});
