import { tokenStore } from '../storage';
import type { ApiEnvelope, ApiErrorBody, TokenPair } from './types';

export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '';

export class ApiError extends Error {
  readonly code: string;
  readonly status: number;
  readonly details: Record<string, string[]> | null;

  constructor(code: string, message: string, status: number, details: Record<string, string[]> | null = null) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
    this.status = status;
    this.details = details;
  }
}

export class NetworkError extends Error {
  constructor(cause?: unknown) {
    super('تعذّر الاتصال بالخادم');
    this.name = 'NetworkError';
    this.cause = cause;
  }
}

/** Validates and normalizes the server envelope; throws ApiError on malformed payloads. */
export function parseEnvelope<T>(raw: unknown): ApiEnvelope<T> {
  if (typeof raw !== 'object' || raw === null || typeof (raw as Record<string, unknown>).success !== 'boolean') {
    throw new ApiError('invalid_response', 'استجابة غير صالحة من الخادم', 0);
  }
  const candidate = raw as Record<string, unknown>;
  const error =
    candidate.error !== null && typeof candidate.error === 'object'
      ? (candidate.error as ApiErrorBody)
      : null;
  return {
    success: candidate.success as boolean,
    data: (candidate.data ?? null) as T | null,
    error,
  };
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  auth?: boolean;
  signal?: AbortSignal;
  isFormData?: boolean;
}

let refreshInFlight: Promise<TokenPair> | null = null;

/** Test helper: clears the single-flight refresh state between test cases. */
export function _resetRefreshState(): void {
  refreshInFlight = null;
}

async function doRefresh(): Promise<TokenPair> {
  const current = tokenStore.get();
  if (!current?.refreshToken) {
    tokenStore.clear();
    throw new ApiError('not_authenticated', 'انتهت الجلسة، يرجى تسجيل الدخول مجدداً', 401);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: current.refreshToken }),
    });
  } catch (cause) {
    throw new NetworkError(cause);
  }

  let envelope: ApiEnvelope<TokenPair> | null = null;
  if (response.ok) {
    try {
      envelope = parseEnvelope<TokenPair>(await response.json());
    } catch {
      envelope = null;
    }
  }

  if (!envelope || !envelope.success || !envelope.data) {
    tokenStore.clear();
    throw new ApiError(
      envelope?.error?.code ?? 'refresh_failed',
      envelope?.error?.message ?? 'انتهت الجلسة، يرجى تسجيل الدخول مجدداً',
      401,
    );
  }

  tokenStore.set(envelope.data);
  return envelope.data;
}

/** Single-flight token refresh: concurrent 401s share one refresh round-trip. */
function refreshTokens(): Promise<TokenPair> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true, signal, isFormData = false } = options;

  const execute = async (): Promise<Response> => {
    const headers: Record<string, string> = {};
    const accessToken = tokenStore.get()?.accessToken;
    if (auth && accessToken) {
      headers.Authorization = `Bearer ${accessToken}`;
    }

    let payload: BodyInit | undefined;
    if (body !== undefined) {
      if (isFormData) {
        payload = body as FormData;
      } else {
        headers['Content-Type'] = 'application/json';
        payload = JSON.stringify(body);
      }
    }

    return fetch(`${API_BASE_URL}${path}`, { method, headers, body: payload, signal });
  };

  let response: Response;
  try {
    response = await execute();
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause;
    throw new NetworkError(cause);
  }

  if (response.status === 401 && auth) {
    await refreshTokens();
    try {
      response = await execute();
    } catch (cause) {
      throw new NetworkError(cause);
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const parsed: unknown = await response.json().catch(() => null);
  const envelope = parseEnvelope<T>(parsed);

  if (!response.ok || !envelope.success) {
    const error: ApiErrorBody = envelope.error ?? {
      code: 'http_error',
      message: `خطأ غير متوقع (${response.status})`,
      details: null,
    };
    throw new ApiError(error.code, error.message, response.status, error.details ?? null);
  }

  return envelope.data as T;
}
