import { Routes, Route } from 'react-router-dom';
import App from '../App';
import PublicLayout from '../layouts/PublicLayout';
import StaffLayout from '../layouts/StaffLayout';
import AdminLayout from '../layouts/AdminLayout';
import CustomerLayout from '../layouts/CustomerLayout';
import RoleGuard from './RoleGuard';
import TicketDetailRoute from './TicketDetailRoute';
import DashboardRedirect from '../pages/dashboard/DashboardRedirect';

import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';
import TrackTicketPage from '../pages/track/TrackTicketPage';
import UnauthorizedPage from '../pages/errors/UnauthorizedPage';
import NotFoundPage from '../pages/errors/NotFoundPage';
import PlaceholderPage from '../pages/PlaceholderPage';
import CustomerListPage from '../pages/customers/CustomerListPage';
import CustomerDetailPage from '../pages/customers/CustomerDetailPage';
import DevicesPage from '../pages/devices/DevicesPage';
import DeviceDetailPage from '../pages/devices/DeviceDetailPage';
import CreateTicketPage from '../pages/tickets/CreateTicketPage';
import TicketListPage from '../pages/tickets/TicketListPage';
import TicketDetailPage from '../pages/tickets/TicketDetailPage';
import TechnicianDashboardPage from '../pages/tickets/TechnicianDashboardPage';
import MyTicketsPage from '../pages/customers/MyTicketsPage';
import StaffDashboardPage from '../pages/dashboard/StaffDashboardPage';
import AdminDashboardPage from '../pages/dashboard/AdminDashboardPage';
import UsersPage from '../pages/admin/UsersPage';

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<App />}>
        {/* ===== Public — không cần đăng nhập ===== */}
        <Route element={<PublicLayout />}>
          <Route index element={<PlaceholderPage title="Trang chủ" />} />
          <Route path="services" element={<PlaceholderPage title="Dịch vụ" />} />
          <Route path="articles" element={<PlaceholderPage title="Bài viết" />} />
          <Route path="track" element={<TrackTicketPage />} />
          <Route path="track/:ticketCode" element={<TrackTicketPage />} />
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
          <Route path="unauthorized" element={<UnauthorizedPage />} />
        </Route>

        {/* ===== /dashboard — điểm trung chuyển sau login ===== */}
        <Route path="dashboard" element={<DashboardRedirect />} />

        {/* ===== Staff: Receptionist + Admin — nghiệp vụ Customer/Device/Ticket/Quote ===== */}
        <Route element={<RoleGuard allowedRoles={['Receptionist', 'Admin']} />}>
          <Route element={<StaffLayout />}>
            <Route path="staff/dashboard" element={<StaffDashboardPage />} />{' '}
            <Route path="devices" element={<DevicesPage />} />
            <Route path="devices/:id" element={<DeviceDetailPage />} />
            <Route path="tickets" element={<TicketListPage />} />{' '}
            <Route path="tickets/create" element={<CreateTicketPage />} />{' '}
            <Route path="customers" element={<CustomerListPage />} />
            <Route path="customers/:id" element={<CustomerDetailPage />} />
          </Route>
        </Route>

        {/* ===== Staff + Customer: xem chi tiết ticket với layout tương ứng ===== */}
        <Route
          element={<RoleGuard allowedRoles={['Receptionist', 'Admin', 'Technician', 'Customer']} />}
        >
          <Route element={<TicketDetailRoute />}>
            <Route path="tickets/:id" element={<TicketDetailPage />} />
          </Route>
        </Route>

        {/* ===== Staff: riêng Technician ===== */}
        <Route element={<RoleGuard allowedRoles={['Technician']} />}>
          <Route element={<StaffLayout />}>
            <Route path="technician/tickets" element={<TechnicianDashboardPage />} />
          </Route>
        </Route>

        {/* ===== Admin ===== */}
        <Route element={<RoleGuard allowedRoles={['Admin']} />}>
          <Route element={<AdminLayout />}>
            <Route path="admin/dashboard" element={<AdminDashboardPage />} />{' '}
            <Route path="admin/users" element={<UsersPage />} />
          </Route>
        </Route>

        {/* ===== Customer ===== */}
        <Route element={<RoleGuard allowedRoles={['Customer']} />}>
          <Route element={<CustomerLayout />}>
            <Route
              path="customer/home"
              element={<PlaceholderPage title="Trang chủ khách hàng" />}
            />
            <Route path="customer/my-tickets" element={<MyTicketsPage />} />{' '}
            <Route path="customer/warranty" element={<PlaceholderPage title="Bảo hành" />} />
            <Route path="customer/profile" element={<PlaceholderPage title="Hồ sơ" />} />
          </Route>
        </Route>

        {/* ===== 404 ===== */}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
