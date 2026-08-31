import { useAuthStore } from '../store/authStore';

// Hook mỏng — che giấu chi tiết Zustand khỏi component, page chỉ cần import useAuth()
export function useAuth() {
  const { role, email, isAuthenticated, login, logout } = useAuthStore();
  return { role, email, isAuthenticated, login, logout };
}