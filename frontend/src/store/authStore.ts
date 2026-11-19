import { create } from 'zustand';
import type { UserRole } from '../types/auth.types';

export const SESSION_FLAG_KEY = 'fixora_has_session';

interface AuthUser {
  id: string;
  email: string;
  role: UserRole;
}

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  userId: string | null;
  role: UserRole | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (accessToken: string, role: UserRole, email: string, userId: string) => void;
  clearSession: () => void;
  logout: () => void;
  hydrate: () => Promise<void>;
}

let expirationTimer: number | undefined;
let hydratePromise: Promise<void> | null = null;

function hasSessionFlag(): boolean {
  try {
    return window.localStorage.getItem(SESSION_FLAG_KEY) === '1';
  } catch {
    return false;
  }
}

function setSessionFlag(active: boolean) {
  try {
    if (active) {
      window.localStorage.setItem(SESSION_FLAG_KEY, '1');
    } else {
      window.localStorage.removeItem(SESSION_FLAG_KEY);
    }
  } catch {
    // Ignore storage access restrictions
  }
}

function clearExpirationTimer() {
  if (expirationTimer !== undefined) {
    window.clearTimeout(expirationTimer);
    expirationTimer = undefined;
  }
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

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  userId: null,
  role: null,
  isAuthenticated: false,
  isHydrated: false,

  login: (accessToken, role, email, userId) => {
    setSessionFlag(true);
    set({
      accessToken,
      role,
      userId,
      user: { id: userId, email, role },
      isAuthenticated: true,
      isHydrated: true,
    });
    scheduleExpiration(accessToken, () => {
      get().clearSession();
    });
  },

  clearSession: () => {
    clearExpirationTimer();
    setSessionFlag(false);
    void import('../services/notificationSignalRService').then(({ notificationSignalRService }) =>
      notificationSignalRService.disconnect()
    );
    set({
      accessToken: null,
      role: null,
      userId: null,
      user: null,
      isAuthenticated: false,
      isHydrated: true,
    });
  },

  logout: () => {
    clearExpirationTimer();
    setSessionFlag(false);
    void import('../services/authService').then(({ authService }) =>
      authService.logout().catch(() => undefined)
    );
    void import('../services/notificationSignalRService').then(({ notificationSignalRService }) =>
      notificationSignalRService.disconnect()
    );
    set({
      accessToken: null,
      role: null,
      userId: null,
      user: null,
      isAuthenticated: false,
      isHydrated: true,
    });
  },

  hydrate: async () => {
    if (!get().accessToken && !hasSessionFlag()) {
      clearExpirationTimer();
      set({
        accessToken: null,
        role: null,
        userId: null,
        user: null,
        isAuthenticated: false,
        isHydrated: true,
      });
      return;
    }

    if (hydratePromise) {
      return hydratePromise;
    }

    hydratePromise = (async () => {
      try {
        const { authService } = await import('../services/authService');
        const response = await authService.refresh();
        setSessionFlag(true);
        set({
          accessToken: response.accessToken,
          role: response.role as UserRole,
          userId: response.userId,
          user: { id: response.userId, email: response.email, role: response.role as UserRole },
          isAuthenticated: true,
          isHydrated: true,
        });
        scheduleExpiration(response.accessToken, () => get().clearSession());
      } catch {
        get().clearSession();
      } finally {
        hydratePromise = null;
      }
    })();

    return hydratePromise;
  },
}));
