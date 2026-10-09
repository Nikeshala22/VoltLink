// -----------------------------------------------------------------------------
// File        : context/AuthContext.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Holds the signed in user for the whole application and restores
//               the session on page load. The only rule this client applies is
//               "is there a valid token"; every permission decision is still
//               made and enforced by the Web API.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { ReactNode } from "react";
import { authApi } from "../api/resources";
import { getToken, setToken } from "../api/client";
import type { User } from "../types";

interface AuthContextValue {
  user: User | null;
  isRestoring: boolean;
  login: (
    email: string,
    password: string,
  ) => Promise<import("../types").LoginResponse>;
  completeLoginWith2Fa: (result: import("../types").LoginResponse) => void;
  updateUser: (updated: User) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(
  undefined,
);

/**
 * Provides the authentication state to the component tree.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);

  // On first load, ask the API who the stored token belongs to. This both
  // restores the session after a refresh and proves the token is still valid.
  useEffect(() => {
    let cancelled = false;

    async function restore() {
      if (!getToken()) {
        setIsRestoring(false);
        return;
      }

      try {
        const profile = await authApi.me();
        if (!cancelled) setUser(profile);
      } catch {
        if (!cancelled) setUser(null);
      } finally {
        if (!cancelled) setIsRestoring(false);
      }
    }

    void restore();

    return () => {
      cancelled = true;
    };
  }, []);

  /**
   * Signs in and keeps both the token and the profile.
   */
  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login(email, password);

    if (!result.requiresTwoFactor && result.accessToken && result.user) {
      setToken(result.accessToken);
      setUser(result.user);
    }

    return result;
  }, []);

  const completeLoginWith2Fa = useCallback(
    (result: import("../types").LoginResponse) => {
      if (result.accessToken && result.user) {
        setToken(result.accessToken);
        setUser(result.user);
      }
    },
    [],
  );

  const updateUser = useCallback((updated: User) => {
    setUser(updated);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({
      user,
      isRestoring,
      login,
      completeLoginWith2Fa,
      updateUser,
      logout,
    }),
    [user, isRestoring, login, completeLoginWith2Fa, updateUser, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
