import type { UserRole } from '../types/auth.types';

export interface NavItem {
  label: string;
  path: string;
  icon?: string; // tên icon (nếu dùng thư viện icon sau này), tạm để text
}

// Tách RIÊNG dữ liệu điều hướng khỏi component Sidebar — khi cần đổi menu chỉ sửa ở đây,
// không đụng vào JSX của layout.
export const NAV_BY_ROLE: Record<UserRole, NavItem[]> = {
  Admin: [
    { label: 'Dashboard', path: '/admin/dashboard' },
    { label: 'Người dùng', path: '/admin/users' },
    { label: 'Khách hàng', path: '/admin/customers' },
    { label: 'Thiết bị', path: '/admin/devices' },
    { label: 'Phiếu sửa chữa', path: '/admin/tickets' },
    { label: 'Báo giá', path: '/admin/quotes' },
    { label: 'Kho linh kiện', path: '/admin/inventory' },
    { label: 'Nội dung Website', path: '/admin/content' },
    { label: 'Báo cáo', path: '/admin/reports' },
  ],
  Receptionist: [
    { label: 'Dashboard', path: '/staff/dashboard' },
    { label: 'Khách hàng', path: '/customers' },
    { label: 'Thiết bị', path: '/devices' },
    { label: 'Phiếu sửa chữa', path: '/tickets' },
    { label: 'Báo giá', path: '/tickets' },
  ],
  Technician: [
    { label: 'Dashboard', path: '/staff/dashboard' },
    { label: 'Ticket của tôi', path: '/staff/my-tickets' },
    { label: 'Công việc sửa chữa', path: '/staff/repair-tasks' },
  ],
  Customer: [
    { label: 'Trang chủ', path: '/customer/home' },
    { label: 'Phiếu của tôi', path: '/customer/my-tickets' },
    { label: 'Bảo hành', path: '/customer/warranty' },
    { label: 'Hồ sơ', path: '/customer/profile' },
  ],
};

export const PUBLIC_NAV: NavItem[] = [
  { label: 'Trang chủ', path: '/' },
  { label: 'Dịch vụ', path: '/services' },
  { label: 'Bài viết', path: '/articles' },
  { label: 'Tra cứu tiến độ', path: '/track' },
];
