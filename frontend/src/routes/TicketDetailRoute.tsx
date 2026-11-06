import { useAuth } from '../hooks/useAuth';
import AdminLayout from '../layouts/AdminLayout';
import CustomerLayout from '../layouts/CustomerLayout';
import StaffLayout from '../layouts/StaffLayout';

export default function TicketDetailRoute() {
  const { role } = useAuth();
  if (role === 'Admin') return <AdminLayout />;
  if (role === 'Customer') return <CustomerLayout />;
  return <StaffLayout />;
}
