import type { UserRole } from '../types/auth.types';

export interface NavItem {
  label: string;
  path: string;
  end?: boolean;
  icon?: string; // tên icon (nếu dùng thư viện icon sau này), tạm để text
}

export interface NavGroup {
  label: string;
  key: string;
  items: NavItem[];
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
    { label: 'Cấu hình lịch hẹn', path: '/admin/appointments/settings' },
    { label: 'Triệu chứng chẩn đoán', path: '/admin/diagnosis-symptoms' },
    { label: 'Kho linh kiện', path: '/admin/inventory' },
    { label: 'Nhập/Xuất kho', path: '/admin/inventory-transactions' },
    { label: 'Dịch vụ', path: '/admin/services' },
    { label: 'Gói bảo hành', path: '/admin/warranty-packages' },
    { label: 'Bài viết', path: '/admin/articles' },
    { label: 'Đánh giá', path: '/admin/reviews' },
    { label: 'Báo cáo doanh thu', path: '/admin/reports/revenue' },
    { label: 'Hiệu suất KTV', path: '/admin/reports/technicians' },
    { label: 'Giám sát SLA', path: '/admin/reports/sla' },
    { label: 'Profit Dashboard', path: '/admin/reports/profit' },
  ],
  Receptionist: [
    { label: 'Dashboard', path: '/staff/dashboard' },
    { label: 'Khách hàng', path: '/customers' },
    { label: 'Thiết bị', path: '/devices' },
    { label: 'Phiếu sửa chữa', path: '/tickets', end: true },
    { label: 'Báo giá', path: '/tickets/quotes' },
    { label: 'Lịch hẹn', path: '/staff/appointments' },
  ],
  Technician: [
    { label: 'Công việc của tôi', path: '/technician/tickets', end: true },
  ],
  Customer: [
    { label: 'Trang chủ', path: '/customer/home' },
    { label: 'Phiếu của tôi', path: '/customer/my-tickets' },
    { label: 'Lịch hẹn', path: '/customer/appointments' },
    { label: 'Bảo hành', path: '/customer/warranty' },
    { label: 'Hồ sơ', path: '/customer/profile' },
  ],
  Manager: [],
  Accountant: [],
};

export const ALLOWED_PATHS_BY_ROLE: Record<UserRole, string[]> = {
  Admin: NAV_BY_ROLE.Admin.map((item) => item.path),
  Manager: [],
  Receptionist: NAV_BY_ROLE.Receptionist.map((item) => item.path),
  Technician: NAV_BY_ROLE.Technician.map((item) => item.path),
  Accountant: [],
  Customer: NAV_BY_ROLE.Customer.map((item) => item.path),
};

const ADMIN_GROUP_DEFINITIONS: Array<{ key: string; label: string; paths: string[] }> = [
  {
    key: 'operations',
    label: 'Vận hành',
    paths: ['/admin/tickets', '/admin/quotes', '/admin/appointments/settings'],
  },
  {
    key: 'catalog',
    label: 'Danh mục',
    paths: ['/admin/customers', '/admin/devices', '/admin/services', '/admin/warranty-packages', '/admin/diagnosis-symptoms'],
  },
  {
    key: 'inventory',
    label: 'Kho',
    paths: ['/admin/inventory', '/admin/inventory-transactions'],
  },
  {
    key: 'analytics',
    label: 'Phân tích',
    paths: ['/admin/reports/revenue', '/admin/reports/technicians', '/admin/reports/sla', '/admin/reports/profit'],
  },
  {
    key: 'content',
    label: 'Nội dung',
    paths: ['/admin/articles', '/admin/reviews'],
  },
  {
    key: 'system',
    label: 'Hệ thống',
    paths: ['/admin/dashboard', '/admin/users'],
  },
];

export const NAV_GROUPS_BY_ROLE: Record<UserRole, NavGroup[]> = {
  Admin: ADMIN_GROUP_DEFINITIONS.map((group) => ({
    key: group.key,
    label: group.label,
    items: group.paths
      .map((path) => NAV_BY_ROLE.Admin.find((item) => item.path === path))
      .filter((item): item is NavItem => item !== undefined),
  })),
  Manager: [],
  Receptionist: [],
  Technician: [],
  Accountant: [],
  Customer: [],
};

export const PUBLIC_NAV: NavItem[] = [
  { label: 'Trang chủ', path: '/' },
  { label: 'Dịch vụ', path: '/services' },
  { label: 'Đặt lịch', path: '/book' },
  { label: 'Tra cứu lịch hẹn', path: '/appointments/lookup' },
  { label: 'Tư vấn giá AI', path: '/estimate' },
  { label: 'Bài viết', path: '/articles' },
  { label: 'Tra cứu tiến độ', path: '/track' },
];
