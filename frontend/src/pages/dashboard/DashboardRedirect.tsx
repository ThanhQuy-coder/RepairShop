import { Navigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

// "/dashboard" không tự render UI — chỉ là điểm trung chuyển sau login,
// tự đưa người dùng tới đúng dashboard theo role của họ (Admin/Staff/Customer khác nhau).
export default function DashboardRedirect() {
  const { role, isAuthenticated } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  switch (role) {
    case 'Admin':
      return <Navigate to="/admin/dashboard" replace />;
    case 'Receptionist':
      return <Navigate to="/staff/dashboard" replace />;
    case 'Technician':
      return <Navigate to="/technician/tickets" replace />;
    case 'Customer':
      return <Navigate to="/customer/home" replace />;
    default:
      return <Navigate to="/login" replace />;
  }
}