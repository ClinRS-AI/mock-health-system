import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState
} from "react";
import {
  clearAdminSession,
  getAdminSessionExpiresAtUtc,
  getAdminSessionToken,
  hydrateAdminSessionFromStorage,
  setAdminSession
} from "./adminSessionStore";
import { exchangeAdminSession, probeAdminKeyRequired } from "./api";

export interface AdminSessionContextValue {
  /** True when a non-expired session token is stored. */
  hasSession: boolean;
  /** ISO 8601 expiry from the server, or null. */
  expiresAtUtc: string | null;
  /** True when the server requires an admin key (protected deployment). */
  isAdminKeyRequired: boolean;
  /** True when the admin-key probe has completed (either outcome). */
  isProbeSettled: boolean;
  /** True when the user is unauthenticated AND admin key protection is active. */
  isDemoMode: boolean;
  /** Exchange the static admin key for a short-lived session JWT. */
  // eslint-disable-next-line no-unused-vars -- parameter name documents the API for consumers
  signIn: (adminKey: string) => Promise<void>;
  signOut: () => void;
  /** Re-read session from storage (e.g. after tab focus). */
  refresh: () => void;
}

const AdminSessionContext = createContext<AdminSessionContextValue | null>(null);

export function AdminSessionProvider({ children }: { children: React.ReactNode }) {
  const [tick, setTick] = useState(0);
  const [isAdminKeyRequired, setIsAdminKeyRequired] = useState(false);
  const [isProbeSettled, setIsProbeSettled] = useState(false);
  const expiryTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const refresh = useCallback(() => {
    setTick((t) => t + 1);
  }, []);

  // Hydrate session from storage and probe open-mode on mount
  useEffect(() => {
    hydrateAdminSessionFromStorage();
    setTick((t) => t + 1);

    let cancelled = false;
    probeAdminKeyRequired()
      .then((required) => {
        if (!cancelled) {
          setIsAdminKeyRequired(required);
          setIsProbeSettled(true);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setIsAdminKeyRequired(true);
          setIsProbeSettled(true);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const hasSession = tick >= 0 && getAdminSessionToken() !== null;
  const expiresAtUtc = tick >= 0 ? getAdminSessionExpiresAtUtc() : null;
  // Consuming components each guard their own mutating handlers with
  // `if (isDemoMode) return;` rather than this being enforced centrally (e.g. in api.ts's
  // request interceptor). Deliberate: the interceptor has no reliable way to distinguish a
  // read from a write per-endpoint, and centralizing would be a cross-cutting change affecting
  // every page, not just Test Data. Per-handler guards are the accepted tradeoff for now.
  const isDemoMode = !hasSession && isAdminKeyRequired;

  // Schedule a timer to fire refresh() exactly when the token expires
  useEffect(() => {
    if (expiryTimerRef.current !== null) {
      clearTimeout(expiryTimerRef.current);
      expiryTimerRef.current = null;
    }

    if (!expiresAtUtc) return;

    const delay = Date.parse(expiresAtUtc) - Date.now();
    if (delay <= 0) return;

    expiryTimerRef.current = setTimeout(() => {
      refresh();
    }, delay);

    return () => {
      if (expiryTimerRef.current !== null) {
        clearTimeout(expiryTimerRef.current);
        expiryTimerRef.current = null;
      }
    };
  }, [expiresAtUtc, refresh]);

  const signIn = useCallback(
    async (adminKey: string) => {
      const result = await exchangeAdminSession(adminKey);
      setAdminSession(result.accessToken, new Date(result.expiresAtUtc));
      refresh();
    },
    [refresh]
  );

  const signOut = useCallback(() => {
    clearAdminSession();
    refresh();
  }, [refresh]);

  const value = useMemo(
    () => ({
      hasSession,
      expiresAtUtc,
      isAdminKeyRequired,
      isProbeSettled,
      isDemoMode,
      signIn,
      signOut,
      refresh
    }),
    [hasSession, expiresAtUtc, isAdminKeyRequired, isProbeSettled, isDemoMode, signIn, signOut, refresh]
  );

  return <AdminSessionContext.Provider value={value}>{children}</AdminSessionContext.Provider>;
}

/** Hook for admin session state; must be used under {@link AdminSessionProvider}. */
// eslint-disable-next-line react-refresh/only-export-components -- hook is paired with provider in this module
export function useAdminSession(): AdminSessionContextValue {
  const ctx = useContext(AdminSessionContext);
  if (!ctx) {
    throw new Error("useAdminSession must be used within AdminSessionProvider");
  }
  return ctx;
}

export default AdminSessionProvider;
