import type { TokenPair } from './api/types';

const TOKEN_KEY = 'khyout.auth.tokens';

function safeParse(raw: string | null): TokenPair | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<TokenPair>;
    if (typeof parsed?.accessToken === 'string' && typeof parsed?.refreshToken === 'string') {
      return {
        accessToken: parsed.accessToken,
        refreshToken: parsed.refreshToken,
        accessTokenExpiresAt: parsed.accessTokenExpiresAt ?? '',
        refreshTokenExpiresAt: parsed.refreshTokenExpiresAt ?? '',
      };
    }
    return null;
  } catch {
    return null;
  }
}

export const tokenStore = {
  get(): TokenPair | null {
    try {
      return safeParse(window.localStorage.getItem(TOKEN_KEY));
    } catch {
      return null;
    }
  },

  set(tokens: TokenPair): void {
    try {
      window.localStorage.setItem(TOKEN_KEY, JSON.stringify(tokens));
    } catch {
      /* storage unavailable (private mode, quota) — session stays in memory only */
    }
  },

  clear(): void {
    try {
      window.localStorage.removeItem(TOKEN_KEY);
    } catch {
      /* ignore */
    }
  },

  isAuthenticated(): boolean {
    return this.get() !== null;
  },
};
