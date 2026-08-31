import { create } from 'zustand';
import type { UserRole } from '../types/auth.types';

interface AuthState {
  accessToken: string | null;
  role: UserRole | null;
  email: string | null;
  isAuthenticated: boolean;
  login: (token: string, role: UserRole, email: string) => void;
  logout: () => void;
  hydrate: () => void; // đọc lại từ localStorage khi reload trang
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  role: null,
  email: null,
  isAuthenticated: false,

  login: (token, role, email) => {
    localStorage.setItem('accessToken', token);
    localStorage.setItem('role', role);
    localStorage.setItem('email', email);
    set({ accessToken: token, role, email, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('role');
    localStorage.removeItem('email');
    set({ accessToken: null, role: null, email: null, isAuthenticated: false });
  },

  hydrate: () => {
    const token = localStorage.getItem('accessToken');
    const role = localStorage.getItem('role') as UserRole | null;
    const email = localStorage.getItem('email');
    if (token && role && email) {
      set({ accessToken: token, role, email, isAuthenticated: true });
    }
  },
}));