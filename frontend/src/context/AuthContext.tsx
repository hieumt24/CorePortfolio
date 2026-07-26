import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { jwtDecode } from 'jwt-decode';
import { profileApi } from '../features/profile/api/profileApi';
import {
  apiClient,
  getAccessToken,
  refreshAccessToken,
  setAccessToken,
} from '../shared/api/baseClient';

interface AuthUser {
  id: string;
  username: string;
  displayName: string;
  email: string | null;
  role: string;
}

interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  login: (token: string) => void;
  logout: () => Promise<void>;
  logoutAll: () => Promise<void>;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isAuthLoading: boolean;
}

interface JwtClaims {
  exp?: number;
  sub?: string;
  name?: string;
  unique_name?: string;
  role?: string;
  [key: string]: string | number | undefined;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const decodeUser = (token: string): AuthUser => {
  const decoded = jwtDecode<JwtClaims>(token);
  const username = String(
    decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
    || decoded.name
    || decoded.unique_name
    || 'User',
  );
  return {
    id: String(
      decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
      || decoded.sub
      || '',
    ),
    username,
    displayName: username,
    email: null,
    role: String(
      decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || decoded.role
      || 'User',
    ),
  };
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setTokenState] = useState<string | null>(getAccessToken());
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isAuthLoading, setIsAuthLoading] = useState(true);

  const applyToken = useCallback((nextToken: string | null) => {
    setAccessToken(nextToken);
    setTokenState(nextToken);
    if (!nextToken) {
      setUser(null);
      return;
    }
    try {
      setUser(decodeUser(nextToken));
    } catch {
      setAccessToken(null);
      setTokenState(null);
      setUser(null);
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiClient<void>('/auth/logout', { method: 'POST' });
    } catch {
      // Local logout must still complete if the API is unavailable.
    } finally {
      applyToken(null);
    }
  }, [applyToken]);

  const logoutAll = useCallback(async () => {
    try {
      await apiClient('/auth/logout-all', { method: 'POST' });
    } finally {
      applyToken(null);
    }
  }, [applyToken]);

  const login = useCallback((newToken: string) => {
    applyToken(newToken);
  }, [applyToken]);

  const refreshUser = useCallback(async () => {
    if (!getAccessToken()) return;
    const profile = await profileApi.get();
    setUser({
      id: profile.id,
      username: profile.username,
      displayName: profile.displayName,
      email: profile.email,
      role: profile.role,
    });
  }, []);

  useEffect(() => {
    localStorage.removeItem('token');
    let active = true;
    void refreshAccessToken()
      .then(refreshedToken => {
        if (active) applyToken(refreshedToken);
      })
      .finally(() => {
        if (active) setIsAuthLoading(false);
      });
    return () => { active = false; };
  }, [applyToken]);

  useEffect(() => {
    const handleUnauthorized = () => applyToken(null);
    const handleTokenRefreshed = (event: Event) =>
      applyToken((event as CustomEvent<string>).detail);
    window.addEventListener('auth:unauthorized', handleUnauthorized);
    window.addEventListener('auth:token-refreshed', handleTokenRefreshed);
    return () => {
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
      window.removeEventListener('auth:token-refreshed', handleTokenRefreshed);
    };
  }, [applyToken]);

  useEffect(() => {
    if (!token) return;
    void profileApi.get()
      .then(profile => {
        setUser({
          id: profile.id,
          username: profile.username,
          displayName: profile.displayName,
          email: profile.email,
          role: profile.role,
        });
      })
      .catch(() => {
        // Keep token-derived identity when profile hydration is temporarily unavailable.
      });
    const decoded = jwtDecode<JwtClaims>(token);
    const expiresAt = (decoded.exp ?? 0) * 1000;
    const refreshIn = Math.max(expiresAt - Date.now() - 30_000, 1_000);
    const timer = window.setTimeout(() => {
      void refreshAccessToken().then(refreshedToken => {
        if (!refreshedToken) applyToken(null);
      });
    }, refreshIn);
    return () => window.clearTimeout(timer);
  }, [applyToken, refreshUser, token]);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        login,
        logout,
        logoutAll,
        refreshUser,
        isAuthenticated: !!token,
        isAdmin: ['Admin', 'SuperAdmin', 'Operations', 'Support', 'MarketDataManager', 'Auditor']
          .includes(user?.role ?? ''),
        isAuthLoading,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

// This file intentionally co-locates the provider with its matching consumer hook.
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) throw new Error('useAuth must be used within an AuthProvider');
  return context;
};
