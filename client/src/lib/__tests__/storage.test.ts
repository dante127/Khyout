import { beforeEach, describe, expect, it } from 'vitest';
import { tokenStore } from '../storage';
import type { TokenPair } from '../api/types';

const pair: TokenPair = {
  accessToken: 'access',
  refreshToken: 'refresh',
  accessTokenExpiresAt: '2026-01-01T00:00:00Z',
  refreshTokenExpiresAt: '2026-02-01T00:00:00Z',
};

describe('tokenStore', () => {
  beforeEach(() => window.localStorage.clear());

  it('round-trips a token pair', () => {
    tokenStore.set(pair);
    expect(tokenStore.get()).toEqual(pair);
  });

  it('returns null when nothing is stored', () => {
    expect(tokenStore.get()).toBeNull();
  });

  it('returns null for corrupt JSON', () => {
    window.localStorage.setItem('khyout.auth.tokens', '{not-json');
    expect(tokenStore.get()).toBeNull();
  });

  it('rejects payloads missing required fields', () => {
    window.localStorage.setItem('khyout.auth.tokens', JSON.stringify({ accessToken: 'only-access' }));
    expect(tokenStore.get()).toBeNull();
  });

  it('clear removes the session and isAuthenticated reflects state', () => {
    tokenStore.set(pair);
    expect(tokenStore.isAuthenticated()).toBe(true);
    tokenStore.clear();
    expect(tokenStore.get()).toBeNull();
    expect(tokenStore.isAuthenticated()).toBe(false);
  });
});
