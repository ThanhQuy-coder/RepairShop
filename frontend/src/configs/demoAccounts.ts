import type { UserRole } from '../types/auth.types';

export interface DemoAccount {
  id: string;
  role: UserRole;
  label: string;
  email: string;
  password: string;
}

const password = import.meta.env.VITE_DEMO_PASSWORD || 'Demo@123';

export const DEMO_ACCOUNTS: DemoAccount[] = [
  {
    id: 'demo-admin',
    role: 'Admin',
    label: 'Quản trị viên',
    email: import.meta.env.VITE_DEMO_ADMIN_EMAIL,
    password: password,
  },
  {
    id: 'demo-receptionist-a',
    role: 'Receptionist',
    label: 'Lễ tân A',
    email: import.meta.env.VITE_DEMO_RECEPTIONIST_A_EMAIL,
    password: password,
  },
  {
    id: 'demo-receptionist-b',
    role: 'Receptionist',
    label: 'Lễ tân B',
    email: import.meta.env.VITE_DEMO_RECEPTIONIST_B_EMAIL,
    password: password,
  },
  {
    id: 'demo-technician',
    role: 'Technician',
    label: 'Kỹ thuật viên',
    email: import.meta.env.VITE_DEMO_TECHNICIAN_EMAIL,
    password: password,
  },
  {
    id: 'demo-customer-a',
    role: 'Customer',
    label: 'Khách hàng A',
    email: import.meta.env.VITE_DEMO_CUSTOMER_A_EMAIL,
    password: password,
  },
  {
    id: 'demo-customer-b',
    role: 'Customer',
    label: 'Khách hàng B',
    email: import.meta.env.VITE_DEMO_CUSTOMER_B_EMAIL,
    password: password,
  },
  {
    id: 'demo-customer-khang',
    role: 'Customer',
    label: 'Khách hàng Khang',
    email: import.meta.env.VITE_DEMO_CUSTOMER_KHANG_EMAIL,
    password: import.meta.env.VITE_KHANG_PASSWORD || password,
  },
];

// Chỉ bật trong môi trường development hoặc khi chủ động cấu hình demo.
export const ENABLE_DEMO_LOGIN =
  import.meta.env.DEV || import.meta.env.VITE_ENABLE_DEMO_LOGIN === 'true';
