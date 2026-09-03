import { useAuth } from '../hooks/useAuth';
import CustomerLayout from '../layouts/CustomerLayout';
import StaffLayout from '../layouts/StaffLayout';

export default function TicketDetailRoute() {
  const { role } = useAuth();
  return role === 'Customer' ? <CustomerLayout /> : <StaffLayout />;
}
