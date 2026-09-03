import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import Loading from '../components/common/Loading';
import type { UserRole } from '../types/auth.types';

interface RoleGuardProps {
  allowedRoles: UserRole[];
}

export default function RoleGuard({ allowedRoles }: RoleGuardProps) {
  const { role, isAuthenticated, isHydrated } = useAuth();

  if (!isHydrated) return <Loading />;

  // Chưa đăng nhập -> về /login (tương đương 401)
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  // Đã đăng nhập nhưng SAI role -> về trang 403, KHÔNG đá về /login
  // (khác hẳn ý nghĩa "chưa đăng nhập" — đây là "biết bạn là ai, nhưng bạn không có quyền")
  if (!role || !allowedRoles.includes(role)) return <Navigate to="/unauthorized" replace />;

  return <Outlet />;
}
