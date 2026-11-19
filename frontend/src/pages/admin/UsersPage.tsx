import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { userService } from '../../services/userService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import {
  Badge,
  Button,
  ErrorMessage,
  Input,
  Modal,
  Pagination,
  Select,
  Table,
  type TableColumn,
} from '../../components/common';
import type { UserListItem } from '../../types/user.types';
import type { UserRole } from '../../types/auth.types';
import styles from './UsersPage.module.css';

const PAGE_SIZE = 8;

const ROLE_OPTIONS: { value: UserRole; label: string; desc: string }[] = [
  {
    value: 'Admin',
    label: 'Admin (Quản trị viên)',
    desc: 'Toàn quyền quản trị hệ thống, người dùng, danh mục và báo cáo.',
  },
  {
    value: 'Receptionist',
    label: 'Receptionist (Lễ tân)',
    desc: 'Tiếp nhận thiết bị, tạo phiếu sửa chữa, báo giá, hóa đơn và điều phối lịch hẹn.',
  },
  {
    value: 'Technician',
    label: 'Technician (Kỹ thuật viên)',
    desc: 'Chẩn đoán lỗi, xuất kho linh kiện, sửa chữa và kiểm định chất lượng QA.',
  },
  {
    value: 'Customer',
    label: 'Customer (Khách hàng)',
    desc: 'Đặt lịch hẹn, theo dõi tiến độ sửa chữa, duyệt báo giá và bảo hành.',
  },
];

export default function UsersPage() {
  const { showSuccess, showError } = useToast();
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [isActive, setIsActive] = useState('');
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [form, setForm] = useState<{
    fullName: string;
    email: string;
    phone: string;
    password: string;
    role: UserRole;
  }>({ fullName: '', email: '', phone: '', password: '', role: 'Technician' });

  const [roleModalUser, setRoleModalUser] = useState<UserListItem | null>(null);
  const [selectedRole, setSelectedRole] = useState<UserRole>('Customer');
  const [isUpdatingRole, setIsUpdatingRole] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const result = await userService.list({
        role: role || undefined,
        isActive: isActive === '' ? undefined : isActive === 'true',
        pageSize: 200,
      });
      setUsers(result.items);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [role, isActive]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [search, role, isActive]);

  const filteredUsers = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return users;
    return users.filter(
      (u) => u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    );
  }, [users, search]);

  const totalPages = Math.max(1, Math.ceil(filteredUsers.length / PAGE_SIZE));

  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  const pagedUsers = useMemo(
    () => filteredUsers.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [filteredUsers, page]
  );

  const createUser = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsCreating(true);
    try {
      await userService.create({ ...form, phone: form.phone || undefined });
      setIsOpen(false);
      setForm({ fullName: '', email: '', phone: '', password: '', role: 'Technician' });
      showSuccess('Tạo tài khoản người dùng thành công.');
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setErrorMessage(msg);
      showError(msg);
    } finally {
      setIsCreating(false);
    }
  };

  const toggleStatus = async (user: UserListItem) => {
    try {
      await userService.setStatus(user.id, !user.isActive);
      showSuccess(
        user.isActive
          ? `Đã khóa tài khoản ${user.fullName}.`
          : `Đã mở khóa tài khoản ${user.fullName}.`
      );
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setErrorMessage(msg);
      showError(msg);
    }
  };

  const openRoleModal = (user: UserListItem) => {
    setRoleModalUser(user);
    setSelectedRole(user.role);
  };

  const handleSaveRole = async () => {
    if (!roleModalUser) return;
    setIsUpdatingRole(true);
    try {
      await userService.setRole(roleModalUser.id, selectedRole);
      showSuccess(`Đã cập nhật vai trò ${selectedRole} cho ${roleModalUser.fullName}.`);
      setRoleModalUser(null);
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setErrorMessage(msg);
      showError(msg);
    } finally {
      setIsUpdatingRole(false);
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
      header: 'Thao tác',
      render: (user) => (
        <div className={styles.actionGroup} onClick={(e) => e.stopPropagation()}>
          <Button variant="secondary" size="sm" onClick={() => openRoleModal(user)}>
            Phân quyền
          </Button>
          <Button variant="ghost" size="sm" onClick={() => void toggleStatus(user)}>
            {user.isActive ? 'Khóa' : 'Mở khóa'}
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>QUẢN TRỊ HỆ THỐNG</span>
          <h1 className={styles.title}>Quản lý người dùng</h1>
          <p className={styles.subtitle}>
            Danh sách tài khoản quản trị viên, nhân viên tiếp nhận, kỹ thuật viên và khách hàng trong hệ thống.
          </p>
        </div>
        <Button size="md" onClick={() => setIsOpen(true)}>
          + Tạo tài khoản
        </Button>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} onRetry={load} />}

      <div className={styles.filterCard}>
        <div className={styles.searchInputWrap}>
          <Input
            aria-label="Tìm kiếm người dùng"
            placeholder="Tìm theo họ tên hoặc email..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <Select
          options={ROLE_OPTIONS.map((item) => ({ value: item.value, label: item.label }))}
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
        data={pagedUsers}
        keyExtractor={(user) => user.id}
        isLoading={isLoading}
        emptyMessage="Chưa có người dùng phù hợp với bộ lọc."
      />

      <Pagination
        page={page}
        pageSize={PAGE_SIZE}
        total={filteredUsers.length}
        onPageChange={setPage}
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
              options={ROLE_OPTIONS.map((item) => ({ value: item.value, label: item.label }))}
              value={form.role}
              onChange={(event) => setForm({ ...form, role: event.target.value as UserRole })}
            />
          </div>
          <div className={styles.modalFooter}>
            <Button
              variant="secondary"
              type="button"
              onClick={() => setIsOpen(false)}
              disabled={isCreating}
            >
              Hủy
            </Button>
            <Button type="submit" isLoading={isCreating}>
              Xác nhận tạo
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={!!roleModalUser}
        onClose={() => {
          if (!isUpdatingRole) setRoleModalUser(null);
        }}
        title="Phân quyền nhanh tài khoản"
        footer={
          <>
            <Button
              variant="secondary"
              type="button"
              onClick={() => setRoleModalUser(null)}
              disabled={isUpdatingRole}
            >
              Hủy
            </Button>
            <Button
              type="button"
              onClick={() => void handleSaveRole()}
              isLoading={isUpdatingRole}
              disabled={!roleModalUser || selectedRole === roleModalUser.role}
            >
              Lưu phân quyền
            </Button>
          </>
        }
      >
        {roleModalUser && (
          <div className={styles.roleModalBody}>
            <div className={styles.roleUserSummary}>
              <div className={styles.userAvatar}>
                {roleModalUser.fullName ? roleModalUser.fullName[0].toUpperCase() : 'U'}
              </div>
              <div className={styles.roleUserMeta}>
                <strong>{roleModalUser.fullName}</strong>
                <span>{roleModalUser.email}</span>
              </div>
              <Badge
                variant={
                  roleModalUser.role === 'Admin'
                    ? 'default'
                    : roleModalUser.role === 'Receptionist'
                      ? 'info'
                      : roleModalUser.role === 'Technician'
                        ? 'warning'
                        : 'success'
                }
              >
                Hiện tại: {roleModalUser.role}
              </Badge>
            </div>

            <div className={styles.roleOptionList} role="radiogroup" aria-label="Chọn vai trò mới">
              {ROLE_OPTIONS.map((opt) => {
                const isSelected = selectedRole === opt.value;
                return (
                  <button
                    key={opt.value}
                    type="button"
                    role="radio"
                    aria-checked={isSelected}
                    className={`${styles.roleOptionCard} ${
                      isSelected ? styles.roleOptionCardSelected : ''
                    }`}
                    onClick={() => setSelectedRole(opt.value)}
                  >
                    <div className={styles.roleOptionHeader}>
                      <strong>{opt.label}</strong>
                      <span className={styles.roleRadioDot} />
                    </div>
                    <p className={styles.roleOptionDesc}>{opt.desc}</p>
                  </button>
                );
              })}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
