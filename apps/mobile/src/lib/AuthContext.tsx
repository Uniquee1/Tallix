import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { hasSession, login, logout } from "./auth";

type Status = "loading" | "signedOut" | "signedIn";

type AuthState = {
  status: Status;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<Status>("loading");

  // On launch: is there a saved session on this device?
  useEffect(() => {
    let cancelled = false;

    hasSession()
      .then((valid) => {
        if (!cancelled) setStatus(valid ? "signedIn" : "signedOut");
      })
      .catch(() => {
        if (!cancelled) setStatus("signedOut");
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const signIn = useCallback(async () => {
    await login();
    setStatus("signedIn");
  }, []);

  const signOut = useCallback(async () => {
    try {
      await logout();
    } finally {
      setStatus("signedOut");
    }
  }, []);

  const value = useMemo(() => ({ status, signIn, signOut }), [status, signIn, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside an AuthProvider.");
  }

  return context;
}