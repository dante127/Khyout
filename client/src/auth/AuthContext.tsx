import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import * as authApi from '../lib/api/auth';
import { tokenStore } from '../lib/storage';
import type { TokenPair } from '../lib/api/types';

export interface AuthState {
  /** True once a token pair is persisted. */
  isAuthenticated: boolean;
  /** Phone number that requested an OTP and is waiting for the code. */
  pendingPhone: string | null;
  requestOtp: (phone: string) => Promise<void>;
  verifyOtp: (code: string) => Promise<void>;
  backToPhone: () => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setAuthenticated] = useState<boolean>(() => tokenStore.isAuthenticated());
  const [pendingPhone, setPendingPhone] = useState<string | null>(null);

  const requestOtp = useCallback(async (phone: string) => {
    await authApi.requestOtp(phone);
    setPendingPhone(phone);
  }, []);

  const verifyOtp = useCallback(
    async (code: string) => {
      if (!pendingPhone) {
        throw new Error('no_pending_phone');
      }
      const pair: TokenPair = await authApi.verifyOtp(pendingPhone, code);
      tokenStore.set(pair);
      setPendingPhone(null);
      setAuthenticated(true);
    },
    [pendingPhone],
  );

  const backToPhone = useCallback(() => setPendingPhone(null), []);

  const logout = useCallback(() => {
    tokenStore.clear();
    setAuthenticated(false);
    setPendingPhone(null);
  }, []);

  const value = useMemo<AuthState>(
    () => ({ isAuthenticated, pendingPhone, requestOtp, verifyOtp, backToPhone, logout }),
    [isAuthenticated, pendingPhone, requestOtp, verifyOtp, backToPhone, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
