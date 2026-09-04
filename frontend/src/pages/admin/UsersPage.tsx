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

const roles: { value: UserRole; label: string }[] = [
  { value: 'Receptionist', label: 'Receptionist' },
  { value: 'Technician', label: 'Technician' },
  { value: 'Customer', label: 'Customer' },
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
    { key: 'fullName', header: 'Họ tên', render: (user) => user.fullName },
    { key: 'email', header: 'Email', render: (user) => user.email },
    { key: 'role', header: 'Vai trò', render: (user) => user.role },
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
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h2>Quản lý người dùng</h2>
        <Button onClick={() => setIsOpen(true)}>+ Tạo tài khoản</Button>
      </div>
      {errorMessage && <ErrorMessage message={errorMessage} onRetry={load} />}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
        <Select
          options={roles}
          placeholder="-- Tất cả vai trò --"
          value={role}
          onChange={(event) => setRole(event.target.value)}
        />
        <Select
          options={[
            { value: 'true', label: 'Hoạt động' },
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
        emptyMessage="Chưa có người dùng."
      />

      <Modal isOpen={isOpen} onClose={() => setIsOpen(false)} title="Tạo tài khoản">
        <form onSubmit={createUser} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Input
            label="Họ tên"
            required
            value={form.fullName}
            onChange={(event) => setForm({ ...form, fullName: event.target.value })}
          />
          <Input
            label="Email"
            type="email"
            required
            value={form.email}
            onChange={(event) => setForm({ ...form, email: event.target.value })}
          />
          <Input
            label="Số điện thoại"
            value={form.phone}
            onChange={(event) => setForm({ ...form, phone: event.target.value })}
          />
          <Input
            label="Mật khẩu"
            type="password"
            required
            value={form.password}
            onChange={(event) => setForm({ ...form, password: event.target.value })}
          />
          <Select
            label="Vai trò"
            options={roles}
            value={form.role}
            onChange={(event) => setForm({ ...form, role: event.target.value as UserRole })}
          />
          <Button type="submit">Tạo tài khoản</Button>
        </form>
      </Modal>
    </div>
  );
}
