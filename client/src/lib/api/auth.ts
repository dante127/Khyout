import { request } from './client';
import type { TokenPair } from './types';

export function requestOtp(phoneNumber: string): Promise<null> {
  return request<null>('/api/v1/auth/otp/request', {
    method: 'POST',
    body: { phoneNumber },
    auth: false,
  });
}

export function verifyOtp(phoneNumber: string, code: string): Promise<TokenPair> {
  return request<TokenPair>('/api/v1/auth/otp/verify', {
    method: 'POST',
    body: { phoneNumber, code },
    auth: false,
  });
}
