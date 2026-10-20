import { type FormEvent, useCallback, useEffect, useState } from 'react';
import apiClient from '../../services/apiClient';
import {
  Button,
  ErrorMessage,
  Input,
  Modal,
  Select,
  Table,
  type TableColumn,
} from '../../components/common';
import { extractApiError } from '../../utils/apiError';

interface TimeSlot {
  id: string;
  dayOfWeek?: number | null;
  slotStart: string;
  slotEnd: string;
  maxCapacity: number;
  isActive: boolean;
}

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

export default function AppointmentSettingsPage() {
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [editing, setEditing] = useState<TimeSlot | null>(null);
  const [form, setForm] = useState({
    dayOfWeek: '',
    slotStart: '08:00',
    slotEnd: '09:00',
    maxCapacity: '5',
  });
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setSlots(await apiClient.get<TimeSlot[]>('/time-slots').then((res) => res.data));
    } catch (err) {
      setError(extractApiError(err).message);
    }
  }, []);
  useEffect(() => {
    void load();
  }, [load]);

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
    try {
      const payload = {
        dayOfWeek: form.dayOfWeek === '' ? null : Number(form.dayOfWeek),
        slotStart: `${form.slotStart}:00`,
        slotEnd: `${form.slotEnd}:00`,
        maxCapacity: Number(form.maxCapacity),
      };
      if (editing) await apiClient.put(`/time-slots/${editing.id}`, payload);
      else await apiClient.post('/time-slots', payload);
      setIsOpen(false);
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  const deactivate = async (slot: TimeSlot) => {
    try {
      await apiClient.delete(`/time-slots/${slot.id}`);
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  const columns: TableColumn<TimeSlot>[] = [
    {
      key: 'day',
      header: 'Ngày áp dụng',
      render: (s) => days.find((d) => d.value === String(s.dayOfWeek ?? ''))?.label ?? 'Mọi ngày',
    },
    {
      key: 'time',
      header: 'Khung giờ',
      render: (s) => `${s.slotStart.slice(0, 5)} – ${s.slotEnd.slice(0, 5)}`,
    },
    { key: 'capacity', header: 'Sức chứa', render: (s) => s.maxCapacity },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (s) => (s.isActive ? 'Đang hoạt động' : 'Đã tắt'),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      render: (s) => (
        <div style={{ display: 'flex', gap: 6 }}>
          <Button size="sm" variant="ghost" onClick={() => openForm(s)}>
            Sửa
          </Button>
          {s.isActive && (
            <Button size="sm" variant="danger" onClick={() => void deactivate(s)}>
              Tắt
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'start',
          marginBottom: 24,
        }}
      >
        <div>
          <span style={{ color: '#64748b', fontSize: 12, letterSpacing: 1 }}>
            QUẢN TRỊ LỊCH HẸN
          </span>
          <h1>Cấu hình thời gian appointment</h1>
          <p style={{ color: '#64748b' }}>Quản lý ngày áp dụng, khung giờ và sức chứa đặt lịch.</p>
        </div>
        <Button onClick={() => openForm()}>+ Thêm khung giờ</Button>
      </div>
      {error && <ErrorMessage message={error} onRetry={() => void load()} />}
      <Table
        columns={columns}
        data={slots}
        keyExtractor={(s) => s.id}
        emptyMessage="Chưa có khung giờ appointment."
      />
      <Modal
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        title={editing ? 'Chỉnh sửa khung giờ' : 'Thêm khung giờ'}
      >
        <form onSubmit={save} style={{ display: 'grid', gap: 14 }}>
          <Select
            label="Ngày áp dụng"
            options={days}
            value={form.dayOfWeek}
            onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}
          />
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
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
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
            <Button variant="secondary" type="button" onClick={() => setIsOpen(false)}>
              Hủy
            </Button>
            <Button type="submit">Lưu cấu hình</Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
