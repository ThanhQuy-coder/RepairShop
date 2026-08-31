import { useAuthStore } from '../store/authStore';

export function useAuth() {
  const { user, role, isAuthenticated, login, logout } = useAuthStore();
  return { user, email: user?.email || null, role, isAuthenticated, login, logout };
}
