import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import type { UserRole } from '../types/auth.types';

interface RoleGuardProps {
  allowedRoles: UserRole[];
}

// Chặn truy cập layout sai role — ví dụ Customer cố vào /staff/... sẽ bị đá về /login (hoặc trang 403).
// Đây là Role-based Authorization ở tầng Frontend — SONG SONG, không thay thế cho Backend
// (Backend Task 7 Tuần 3 vẫn là lớp bảo vệ thật sự; Frontend chỉ để UX tốt hơn, tránh render nhầm layout).
export default function RoleGuard({ allowedRoles }: RoleGuardProps) {
  const { role, isAuthenticated } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!role || !allowedRoles.includes(role)) return <Navigate to="/login" replace />;

  return <Outlet />;
}
