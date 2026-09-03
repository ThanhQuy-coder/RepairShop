import { useAuthStore } from '../store/authStore';

export function useAuth() {
  const { user, role, isAuthenticated, isHydrated, login, logout } = useAuthStore();
  return { user, email: user?.email || null, role, isAuthenticated, isHydrated, login, logout };
}
