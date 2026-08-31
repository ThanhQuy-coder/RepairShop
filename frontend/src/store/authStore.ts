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
  login: (accessToken: string, role: UserRole, email: string) => void;
  logout: () => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  role: null,
  isAuthenticated: false,

  login: (accessToken, role, email) => {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('role', role);
    localStorage.setItem('email', email);
    set({ accessToken, role, user: { email, role }, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('role');
    localStorage.removeItem('email');
    set({ accessToken: null, role: null, user: null, isAuthenticated: false });
  },

  hydrate: () => {
    const accessToken = localStorage.getItem('accessToken');
    const role = localStorage.getItem('role') as UserRole | null;
    const email = localStorage.getItem('email');
    if (accessToken && role && email) {
      set({ accessToken, role, user: { email, role }, isAuthenticated: true });
    }
  },
}));