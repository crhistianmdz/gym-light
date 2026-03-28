import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  login as apiLogin,
  logout as apiLogout,
  refreshToken as apiRefresh,
  type LoginPayload,
} from '@/services/authService';
import { initHttpClient } from '@/services/httpClient';
import { syncService } from '@/services/syncService';
import type { AuthSession } from '@/types/auth';

interface AuthContextValue {
  user: AuthSession | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (payload: LoginPayload) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser]       = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const tokenRef = useRef<string | null>(null);

  const doLogout = useCallback(async () => {
    await apiLogout();
    tokenRef.current = null;
    setUser(null);
  }, []);

  const doRefresh = useCallback(async (): Promise<string> => {
    const data = await apiRefresh();
    tokenRef.current = data.accessToken;
    setUser({
      userId:      data.userId,
      fullName:    data.fullName,
      role:        data.role,
      accessToken: data.accessToken,
      expiresAt:   data.expiresAt,
    });
    return data.accessToken;
  }, []);

  // Wire httpClient once
  useEffect(() => {
    initHttpClient(
      () => tokenRef.current,
      doRefresh,
      () => { void doLogout(); },
    );
  }, [doRefresh, doLogout]);

  // Start SyncService — runs for the lifetime of the app
  useEffect(() => {
    syncService.startSync();

    // Pause sync queue on auth-required event (token expired mid-sync)
    const handleAuthRequired = () => { void doLogout(); };
    window.addEventListener('sync:auth-required', handleAuthRequired);

    return () => {
      syncService.stopSync();
      window.removeEventListener('sync:auth-required', handleAuthRequired);
    };
  }, [doLogout]);

  // Silent refresh on mount to restore session from HttpOnly cookie
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await doRefresh();
      } catch {
        // No active session — stay logged out
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [doRefresh]);

  const login = useCallback(async (payload: LoginPayload) => {
    const data = await apiLogin(payload);
    tokenRef.current = data.accessToken;
    setUser({
      userId:      data.userId,
      fullName:    data.fullName,
      role:        data.role,
      accessToken: data.accessToken,
      expiresAt:   data.expiresAt,
    });
  }, []);

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: user !== null,
      isLoading,
      login,
      logout: doLogout,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}