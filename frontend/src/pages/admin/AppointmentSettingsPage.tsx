import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import apiClient from '../../services/apiClient';
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
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import styles from './AppointmentSettingsPage.module.css';

interface TimeSlot {
  id: string;
  dayOfWeek?: number | null;
  slotStart: string;
  slotEnd: string;
  maxCapacity: number;
  isActive: boolean;
}

const PAGE_SIZE = 6;

const days = [
  { value: '', label: 'Mọi ngày' },
  { value: '1', label: 'Thứ 2' },
  { value: '2', label: 'Thứ 3' },
  { value: '3', label: 'Thứ 4' },
  { value: '4', label: 'Thứ 5' },
  { value: '5', label: 'Thứ 6' },
  { value: '6', label: 'Thứ 7' },
  { value: '0', label: 'Chủ nhật' },
];

const dayFilterOptions = [
  { value: 'all', label: 'Mọi ngày (mặc định)' },
  { value: '1', label: 'Thứ 2' },
  { value: '2', label: 'Thứ 3' },
  { value: '3', label: 'Thứ 4' },
  { value: '4', label: 'Thứ 5' },
  { value: '5', label: 'Thứ 6' },
  { value: '6', label: 'Thứ 7' },
  { value: '0', label: 'Chủ nhật' },
];

export default function AppointmentSettingsPage() {
  const { showSuccess, showError } = useToast();
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [dayFilter, setDayFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);

  const [isOpen, setIsOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [editing, setEditing] = useState<TimeSlot | null>(null);
  const [form, setForm] = useState({
    dayOfWeek: '',
    slotStart: '08:00',
    slotEnd: '09:00',
    maxCapacity: '5',
  });
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setSlots(await apiClient.get<TimeSlot[]>('/time-slots').then((res) => res.data));
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [dayFilter, statusFilter]);

  const filteredSlots = useMemo(() => {
    return slots.filter((slot) => {
      if (dayFilter === 'all' && slot.dayOfWeek != null) return false;
      if (dayFilter !== '' && dayFilter !== 'all' && String(slot.dayOfWeek ?? '') !== dayFilter) {
        return false;
      }
      if (statusFilter === 'active' && !slot.isActive) return false;
      if (statusFilter === 'inactive' && slot.isActive) return false;
      return true;
    });
  }, [slots, dayFilter, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredSlots.length / PAGE_SIZE));

  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  const pagedSlots = useMemo(
    () => filteredSlots.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [filteredSlots, page]
  );

  const openForm = (slot?: TimeSlot) => {
    setEditing(slot ?? null);
    setForm(
      slot
        ? {
            dayOfWeek: slot.dayOfWeek == null ? '' : String(slot.dayOfWeek),
            slotStart: slot.slotStart.slice(0, 5),
            slotEnd: slot.slotEnd.slice(0, 5),
            maxCapacity: String(slot.maxCapacity),
          }
        : { dayOfWeek: '', slotStart: '08:00', slotEnd: '09:00', maxCapacity: '5' }
    );
    setIsOpen(true);
  };

  const save = async (event: FormEvent) => {
    event.preventDefault();
    setIsSaving(true);
    try {
      const payload = {
        dayOfWeek: form.dayOfWeek === '' ? null : Number(form.dayOfWeek),
        slotStart: `${form.slotStart}:00`,
        slotEnd: `${form.slotEnd}:00`,
        maxCapacity: Number(form.maxCapacity),
      };
      if (editing) {
        await apiClient.put(`/time-slots/${editing.id}`, payload);
        showSuccess('Đã cập nhật cấu hình khung giờ.');
      } else {
        await apiClient.post('/time-slots', payload);
        showSuccess('Đã thêm khung giờ mới.');
      }
      setIsOpen(false);
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setError(msg);
      showError(msg);
    } finally {
      setIsSaving(false);
    }
  };

  const deactivate = async (slot: TimeSlot) => {
    try {
      await apiClient.delete(`/time-slots/${slot.id}`);
      showSuccess('Đã tắt khung giờ lịch hẹn.');
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setError(msg);
      showError(msg);
    }
  };

  const activate = async (slot: TimeSlot) => {
    try {
      await apiClient.post(`/time-slots/${slot.id}/activate`);
      showSuccess('Đã kích hoạt lại khung giờ lịch hẹn.');
      await load();
    } catch (err) {
      const msg = extractApiError(err).message;
      setError(msg);
      showError(msg);
    }
  };

  const columns: TableColumn<TimeSlot>[] = [
    {
      key: 'day',
      header: 'Ngày áp dụng',
      render: (s) => (
        <strong>
          {days.find((d) => d.value === String(s.dayOfWeek ?? ''))?.label ?? 'Mọi ngày'}
        </strong>
      ),
    },
    {
      key: 'time',
      header: 'Khung giờ',
      render: (s) => (
        <span className={styles.timeSlotTag}>
          {s.slotStart.slice(0, 5)} – {s.slotEnd.slice(0, 5)}
        </span>
      ),
    },
    { key: 'capacity', header: 'Sức chứa', render: (s) => `${s.maxCapacity} lượt` },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (s) => (
        <Badge variant={s.isActive ? 'success' : 'danger'}>
          {s.isActive ? 'Đang hoạt động' : 'Đã tắt'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      render: (s) => (
        <div className={styles.actionGroup}>
          <Button size="sm" variant="ghost" onClick={() => openForm(s)}>
            Sửa
          </Button>
          {s.isActive ? (
            <Button size="sm" variant="danger" onClick={() => void deactivate(s)}>
              Tắt
            </Button>
          ) : (
            <Button size="sm" variant="success" onClick={() => void activate(s)}>
              Bật lại
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>QUẢN TRỊ LỊCH HẸN</span>
          <h1 className={styles.title}>Cấu hình thời gian appointment</h1>
          <p className={styles.subtitle}>
            Quản lý ngày áp dụng, khung giờ tiếp nhận và sức chứa đặt lịch tại cửa hàng.
          </p>
        </div>
        <Button onClick={() => openForm()}>+ Thêm khung giờ</Button>
      </div>

      {error && <ErrorMessage message={error} onRetry={() => void load()} />}

      <div className={styles.filterCard}>
        <Select
          options={dayFilterOptions}
          placeholder="-- Tất cả ngày áp dụng --"
          value={dayFilter}
          onChange={(e) => setDayFilter(e.target.value)}
        />
        <Select
          options={[
            { value: 'active', label: 'Đang hoạt động' },
            { value: 'inactive', label: 'Đã tắt' },
          ]}
          placeholder="-- Tất cả trạng thái --"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
        />
        {(dayFilter || statusFilter) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setDayFilter('');
              setStatusFilter('');
            }}
          >
            Xóa lọc
          </Button>
        )}
      </div>

      <Table
        columns={columns}
        data={pagedSlots}
        keyExtractor={(s) => s.id}
        isLoading={isLoading}
        emptyMessage="Chưa có khung giờ appointment phù hợp."
      />

      <Pagination
        page={page}
        pageSize={PAGE_SIZE}
        total={filteredSlots.length}
        onPageChange={setPage}
      />

      <Modal
        isOpen={isOpen}
        onClose={() => {
          if (!isSaving) setIsOpen(false);
        }}
        title={editing ? 'Chỉnh sửa khung giờ' : 'Thêm khung giờ'}
      >
        <form onSubmit={save} className={styles.formGrid}>
          <Select
            label="Ngày áp dụng"
            options={days}
            value={form.dayOfWeek}
            onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}
          />
          <div className={styles.formRow}>
            <Input
              label="Bắt đầu"
              type="time"
              required
              value={form.slotStart}
              onChange={(e) => setForm({ ...form, slotStart: e.target.value })}
            />
            <Input
              label="Kết thúc"
              type="time"
              required
              value={form.slotEnd}
              onChange={(e) => setForm({ ...form, slotEnd: e.target.value })}
            />
          </div>
          <Input
            label="Sức chứa"
            type="number"
            min="1"
            required
            value={form.maxCapacity}
            onChange={(e) => setForm({ ...form, maxCapacity: e.target.value })}
          />
          <div className={styles.modalFooter}>
            <Button
              variant="secondary"
              type="button"
              onClick={() => setIsOpen(false)}
              disabled={isSaving}
            >
              Hủy
            </Button>
            <Button type="submit" isLoading={isSaving}>
              Lưu cấu hình
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
