import { useAuthStore } from '../store/authStore';

export function useAuth() {
  const { user, role, isAuthenticated, login, logout } = useAuthStore();
  return { user, role, isAuthenticated, login, logout };
}
