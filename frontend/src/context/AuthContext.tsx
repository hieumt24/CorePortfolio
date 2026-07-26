import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { jwtDecode } from 'jwt-decode';
import { profileApi } from '../features/profile/api/profileApi';

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
  logout: () => void;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [user, setUser] = useState<AuthUser | null>(null);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  }, []);

  const login = useCallback((newToken: string) => {
    localStorage.setItem('token', newToken);
    setToken(newToken);
  }, []);

  const refreshUser = useCallback(async () => {
    if (!localStorage.getItem('token')) return;
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
    const handleUnauthorized = () => logout();
    window.addEventListener('auth:unauthorized', handleUnauthorized);

    if (token) {
      try {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const decoded: any = jwtDecode(token);
        const username =
          decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || decoded.name
          || decoded.unique_name
          || 'User';
        const decodedUser: AuthUser = {
          id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.sub,
          username,
          displayName: username,
          email: null,
          role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role,
        };

        if (!decoded.exp || decoded.exp * 1000 <= Date.now()) {
          // Token expiry is the external event this effect synchronizes into auth state.
          // eslint-disable-next-line react-hooks/set-state-in-effect
          logout();
        } else {
          // Token replacement is the external event this effect synchronizes into user state.
          setUser(decodedUser);
          void refreshUser().catch(() => {
            // Keep the token-derived identity when profile hydration is temporarily unavailable.
          });
          const expiresIn = decoded.exp * 1000 - Date.now();
          const expiryTimer = window.setTimeout(logout, expiresIn);
          return () => {
            window.clearTimeout(expiryTimer);
            window.removeEventListener('auth:unauthorized', handleUnauthorized);
          };
        }
      } catch (e) {
        console.error('Invalid token', e);
        // Invalid token parsing is the external event this effect synchronizes into auth state.
        logout();
      }
    } else {
      // Removed storage token is the external event this effect synchronizes into user state.
      setUser(null);
    }

    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized);
  }, [token, logout, refreshUser]);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        login,
        logout,
        refreshUser,
        isAuthenticated: !!token,
        isAdmin: ['Admin', 'SuperAdmin', 'Operations', 'Support', 'MarketDataManager', 'Auditor']
          .includes(user?.role ?? ''),
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
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
