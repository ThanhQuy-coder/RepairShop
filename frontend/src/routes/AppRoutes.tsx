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
import PartsListPage from '../pages/inventory/PartsListPage';
import InventoryTransactionsPage from '../pages/inventory/InventoryTransactionsPage';
import RevenueReportPage from '../pages/reports/RevenueReportPage';
import TechnicianPerformancePage from '../pages/reports/TechnicianPerformancePage';
import MyWarrantyPage from '../pages/customers/MyWarrantyPage';
import ServicesPage from '../pages/website/ServicesPage';
import ArticlesPage from '../pages/website/ArticlesPage';
import ReviewsAdminPage from '../pages/admin/ReviewsAdminPage';

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<App />}>
        {/* ===== Public — không cần đăng nhập ===== */}
        <Route element={<PublicLayout />}>
          <Route index element={<PlaceholderPage title="Trang chủ" />} />
          <Route path="services" element={<ServicesPage />} />
          <Route path="articles" element={<ArticlesPage />} />
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
            <Route path="admin/inventory" element={<PartsListPage />} />
            <Route path="admin/inventory-transactions" element={<InventoryTransactionsPage />} />
            <Route path="admin/reports/revenue" element={<RevenueReportPage />} />
            <Route path="admin/reports/technicians" element={<TechnicianPerformancePage />} />
            <Route path="admin/reviews" element={<ReviewsAdminPage />} />
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
            <Route path="customer/profile" element={<PlaceholderPage title="Hồ sơ" />} />
            <Route path="customer/warranty" element={<MyWarrantyPage />} />
          </Route>
        </Route>

        {/* ===== 404 ===== */}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
