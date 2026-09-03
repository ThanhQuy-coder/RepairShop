import { create } from 'zustand';
import type { UserRole } from '../types/auth.types';

interface AuthUser {
  email: string;
  role: UserRole;
}

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  role: UserRole | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (accessToken: string, role: UserRole, email: string) => void;
  logout: () => void;
  hydrate: () => void;
}

let expirationTimer: number | undefined;

function clearExpirationTimer() {
  if (expirationTimer !== undefined) {
    window.clearTimeout(expirationTimer);
    expirationTimer = undefined;
  }
}

function clearStoredSession() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('role');
  localStorage.removeItem('email');
}

function getTokenExpiration(token: string): number | null {
  try {
    const payload = token.split('.')[1];
    const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padding = '='.repeat((4 - (normalizedPayload.length % 4)) % 4);
    const decoded = JSON.parse(window.atob(normalizedPayload + padding));
    return typeof decoded.exp === 'number' ? decoded.exp * 1000 : null;
  } catch {
    return null;
  }
}

function scheduleExpiration(token: string, onExpired: () => void) {
  clearExpirationTimer();
  const expiration = getTokenExpiration(token);
  if (expiration === null) return;

  const delay = expiration - Date.now();
  if (delay <= 0) {
    onExpired();
    return;
  }

  expirationTimer = window.setTimeout(onExpired, delay);
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  role: null,
  isAuthenticated: false,
  isHydrated: false,

  login: (accessToken, role, email) => {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('role', role);
    localStorage.setItem('email', email);
    set({ accessToken, role, user: { email, role }, isAuthenticated: true, isHydrated: true });
    scheduleExpiration(accessToken, () => {
      clearStoredSession();
      set({ accessToken: null, role: null, user: null, isAuthenticated: false, isHydrated: true });
    });
  },

  logout: () => {
    clearExpirationTimer();
    clearStoredSession();
    set({ accessToken: null, role: null, user: null, isAuthenticated: false, isHydrated: true });
  },

  hydrate: () => {
    const accessToken = localStorage.getItem('accessToken');
    const role = localStorage.getItem('role') as UserRole | null;
    const email = localStorage.getItem('email');
    if (accessToken && role && email) {
      set({ accessToken, role, user: { email, role }, isAuthenticated: true, isHydrated: true });
      scheduleExpiration(accessToken, () => {
        clearStoredSession();
        set({
          accessToken: null,
          role: null,
          user: null,
          isAuthenticated: false,
          isHydrated: true,
        });
      });
    } else {
      clearExpirationTimer();
      clearStoredSession();
      set({ isHydrated: true });
    }
  },
}));
