import { type FormEvent, useCallback, useEffect, useState } from 'react';
import { userService } from '../../services/userService';
import { extractApiError } from '../../utils/apiError';
import {
  Badge,
  Button,
  ErrorMessage,
  Input,
  Modal,
  Select,
  Table,
  type TableColumn,
} from '../../components/common';
import type { UserListItem } from '../../types/user.types';
import type { UserRole } from '../../types/auth.types';
import styles from './UsersPage.module.css';

const roles: { value: UserRole; label: string }[] = [
  { value: 'Receptionist', label: 'Receptionist (Lễ tân)' },
  { value: 'Technician', label: 'Technician (Kỹ thuật viên)' },
  { value: 'Customer', label: 'Customer (Khách hàng)' },
];

export default function UsersPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [role, setRole] = useState('');
  const [isActive, setIsActive] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [form, setForm] = useState<{
    fullName: string;
    email: string;
    phone: string;
    password: string;
    role: UserRole;
  }>({ fullName: '', email: '', phone: '', password: '', role: 'Technician' });

  const load = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const result = await userService.list({
        role: role || undefined,
        isActive: isActive === '' ? undefined : isActive === 'true',
        pageSize: 100,
      });
      setUsers(result.items);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [role, isActive]);

  useEffect(() => {
    load();
  }, [load]);

  const createUser = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    try {
      await userService.create({ ...form, phone: form.phone || undefined });
      setIsOpen(false);
      setForm({ fullName: '', email: '', phone: '', password: '', role: 'Technician' });
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const toggleStatus = async (user: UserListItem) => {
    try {
      await userService.setStatus(user.id, !user.isActive);
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const columns: TableColumn<UserListItem>[] = [
    {
      key: 'fullName',
      header: 'Họ tên',
      render: (user) => (
        <div className={styles.userCell}>
          <div className={styles.userAvatar}>
            {user.fullName ? user.fullName[0].toUpperCase() : 'U'}
          </div>
          <span className={styles.userName}>{user.fullName}</span>
        </div>
      ),
    },
    { key: 'email', header: 'Email', render: (user) => user.email },
    {
      key: 'role',
      header: 'Vai trò',
      render: (user) => (
        <Badge
          variant={
            user.role === 'Admin'
              ? 'default'
              : user.role === 'Receptionist'
                ? 'info'
                : user.role === 'Technician'
                  ? 'warning'
                  : 'success'
          }
        >
          {user.role}
        </Badge>
      ),
    },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (user) => (
        <Badge variant={user.isActive ? 'success' : 'danger'}>
          {user.isActive ? 'Hoạt động' : 'Đã khóa'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: '',
      render: (user) => (
        <Button variant="ghost" size="sm" onClick={() => toggleStatus(user)}>
          {user.isActive ? 'Khóa' : 'Mở khóa'}
        </Button>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>QUẢN TRỊ HỆ THỐNG</span>
          <h1 className={styles.title}>Quản lý người dùng</h1>
          <p className={styles.subtitle}>Danh sách tài khoản nhân viên, kỹ thuật viên và khách hàng trong hệ thống.</p>
        </div>
        <Button size="md" onClick={() => setIsOpen(true)}>
          + Tạo tài khoản
        </Button>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} onRetry={load} />}

      <div className={styles.filterCard}>
        <Select
          options={roles}
          placeholder="-- Tất cả vai trò --"
          value={role}
          onChange={(event) => setRole(event.target.value)}
        />
        <Select
          options={[
            { value: 'true', label: 'Đang hoạt động' },
            { value: 'false', label: 'Đã khóa' },
          ]}
          placeholder="-- Tất cả trạng thái --"
          value={isActive}
          onChange={(event) => setIsActive(event.target.value)}
        />
      </div>

      <Table
        columns={columns}
        data={users}
        keyExtractor={(user) => user.id}
        isLoading={isLoading}
        emptyMessage="Chưa có người dùng phù hợp với bộ lọc."
      />

      <Modal isOpen={isOpen} onClose={() => setIsOpen(false)} title="Tạo tài khoản người dùng">
        <form onSubmit={createUser} className={styles.formGrid}>
          <Input
            label="Họ tên"
            required
            placeholder="Ví dụ: Nguyễn Văn A"
            value={form.fullName}
            onChange={(event) => setForm({ ...form, fullName: event.target.value })}
          />
          <div className={styles.formRow}>
            <Input
              label="Email đăng nhập"
              type="email"
              required
              placeholder="user@repairshop.vn"
              value={form.email}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
            />
            <Input
              label="Số điện thoại"
              placeholder="0912345678"
              value={form.phone}
              onChange={(event) => setForm({ ...form, phone: event.target.value })}
            />
          </div>
          <div className={styles.formRow}>
            <Input
              label="Mật khẩu khởi tạo"
              type="password"
              required
              placeholder="Tối thiểu 6 ký tự"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
            />
            <Select
              label="Vai trò tài khoản"
              options={roles}
              value={form.role}
              onChange={(event) => setForm({ ...form, role: event.target.value as UserRole })}
            />
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 12 }}>
            <Button variant="secondary" type="button" onClick={() => setIsOpen(false)}>
              Hủy
            </Button>
            <Button type="submit">
              Xác nhận tạo
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

