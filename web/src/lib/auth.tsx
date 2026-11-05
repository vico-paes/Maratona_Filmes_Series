"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, useSyncExternalStore } from "react";
import { api, getToken, setToken, tokenEvent } from "./api";
import type { Me } from "./types";

type AuthValue = {
  me: Me | null;
  ready: boolean;
  signIn: (token: string, me: Me) => void;
  signOut: () => void;
  setMe: (me: Me) => void;
};

const AuthContext = createContext<AuthValue | null>(null);

function subscribe(callback: () => void) {
  window.addEventListener(tokenEvent, callback);
  window.addEventListener("storage", callback);
  return () => {
    window.removeEventListener(tokenEvent, callback);
    window.removeEventListener("storage", callback);
  };
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const token = useSyncExternalStore(subscribe, getToken, () => undefined);
  const [loaded, setLoaded] = useState<{ token: string; me: Me | null } | null>(null);

  useEffect(() => {
    if (!token) return;
    let active = true;
    api<Me>("/me").then(
      (me) => active && setLoaded({ token, me }),
      () => active && setLoaded({ token, me: null }),
    );
    return () => {
      active = false;
    };
  }, [token]);

  const signIn = useCallback((newToken: string, me: Me) => {
    setLoaded({ token: newToken, me });
    setToken(newToken);
  }, []);

  const signOut = useCallback(() => setToken(null), []);

  const setMe = useCallback((me: Me) => setLoaded((current) => (current ? { ...current, me } : current)), []);

  const value = useMemo<AuthValue>(() => {
    const current = token && loaded?.token === token ? loaded : null;
    return {
      me: current?.me ?? null,
      ready: token === null || current !== null,
      signIn,
      signOut,
      setMe,
    };
  }, [token, loaded, signIn, signOut, setMe]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("AuthProvider ausente");
  return value;
}
