import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, ErrorMessage, Input, Loading } from '../../components/common';
import { extractApiError } from '../../utils/apiError';
import { reportsService, type SlaPolicy } from '../../services/reportsService';
import styles from './SlaPoliciesAdminPage.module.css';

const DEVICE_TYPES = [
  { value: '', label: 'Tất cả loại thiết bị' },
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử' },
  { value: 'Tablet', label: 'Máy tính bảng' },
  { value: 'Smartwatch', label: 'Đồng hồ thông minh' },
];

const DEVICE_LABELS = DEVICE_TYPES.reduce<Record<string, string>>((labels, item, index) => {
  if (item.value) labels[String(index - 1)] = item.label;
  return labels;
}, {});

const STATUS_OPTIONS = [
  'CHECKED_IN',
  'ASSIGNED',
  'DIAGNOSING',
  'WAITING_APPROVAL',
  'ON_HOLD',
  'WAITING_PARTS',
  'IN_REPAIR',
  'QA_TESTING',
  'READY_FOR_PICKUP',
];

const PAGE_SIZE = 8;

function formatDuration(minutes: number) {
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  if (hours === 0) return `${remainingMinutes} phút`;
  if (remainingMinutes === 0) return `${hours} giờ`;
  return `${hours} giờ ${remainingMinutes} phút`;
}

export default function SlaPoliciesAdminPage() {
  const [policies, setPolicies] = useState<SlaPolicy[]>([]);
  const [statusCode, setStatusCode] = useState(STATUS_OPTIONS[0]);
  const [deviceType, setDeviceType] = useState('');
  const [durationHours, setDurationHours] = useState('24');
  const [durationMinutes, setDurationMinutes] = useState('0');
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      setPolicies(await reportsService.getSlaPolicies());
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const totalPages = Math.max(1, Math.ceil(policies.length / PAGE_SIZE));
  const pagedPolicies = useMemo(
    () => policies.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [page, policies]
  );

  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  const create = async () => {
    const hours = Number(durationHours);
    const minutes = Number(durationMinutes);
    if (!Number.isInteger(hours) || hours < 0 || !Number.isInteger(minutes) || minutes < 0 || minutes > 59 ||
      (hours === 0 && minutes === 0)) {
      setError('Thời hạn SLA phải gồm giờ/phút hợp lệ và lớn hơn 0.');
      return;
    }

    setIsSaving(true);
    try {
      await reportsService.createSlaPolicy({
        statusCode,
        deviceType: deviceType || null,
        durationMinutes: hours * 60 + minutes,
      });
      await load();
      setPage(1);
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const toggle = async (policy: SlaPolicy) => {
    try {
      await reportsService.updateSlaPolicy(policy.id, {
        durationMinutes: policy.durationMinutes,
        isActive: !policy.isActive,
      });
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <span className={styles.eyebrow}>CẤU HÌNH SLA</span>
        <h1>Thời hạn xử lý theo trạng thái</h1>
        <p>Thiết lập thời hạn theo giờ và phút. Chính sách theo loại thiết bị được ưu tiên hơn chính sách dùng chung.</p>
      </header>

      {error && <ErrorMessage message={error} onRetry={() => void load()} />}

      <section className={styles.formCard}>
        <h2>Thêm chính sách SLA</h2>
        <div className={styles.formGrid}>
          <label>
            Trạng thái
            <select value={statusCode} onChange={(event) => setStatusCode(event.target.value)}>
              {STATUS_OPTIONS.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </label>
          <label>
            Loại thiết bị
            <select value={deviceType} onChange={(event) => setDeviceType(event.target.value)}>
              {DEVICE_TYPES.map((device) => <option key={device.value} value={device.value}>{device.label}</option>)}
            </select>
          </label>
          <Input
            label="Số giờ"
            type="number"
            min="0"
            step="1"
            value={durationHours}
            onChange={(event) => setDurationHours(event.target.value)}
          />
          <Input
            label="Số phút"
            type="number"
            min="0"
            max="59"
            step="1"
            value={durationMinutes}
            onChange={(event) => setDurationMinutes(event.target.value)}
          />
          <Button onClick={() => void create()} isLoading={isSaving}>Thêm chính sách</Button>
        </div>
      </section>

      <section className={styles.listCard}>
        <div className={styles.listHeader}>
          <h2>Danh sách chính sách</h2>
          <Button variant="secondary" size="sm" onClick={() => void load()}>Làm mới</Button>
        </div>
        {isLoading ? <Loading /> : policies.length === 0 ? (
          <p className={styles.empty}>Chưa có chính sách SLA.</p>
        ) : (
          <div className={styles.tableWrap}>
            <table>
              <thead><tr><th>Trạng thái</th><th>Thiết bị</th><th>Thời hạn</th><th>Trạng thái cấu hình</th><th /></tr></thead>
              <tbody>
                {pagedPolicies.map((policy) => (
                  <tr key={policy.id}>
                    <td><strong>{policy.statusCode}</strong></td>
                    <td>{policy.deviceType === null ? 'Tất cả' : DEVICE_LABELS[String(policy.deviceType)] ?? `Loại ${policy.deviceType}`}</td>
                    <td><strong>{formatDuration(policy.durationMinutes)}</strong></td>
                    <td><span className={policy.isActive ? styles.active : styles.inactive}>{policy.isActive ? 'Đang áp dụng' : 'Tạm ngừng'}</span></td>
                    <td><Button size="sm" variant="secondary" onClick={() => void toggle(policy)}>{policy.isActive ? 'Tạm ngừng' : 'Kích hoạt'}</Button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {!isLoading && policies.length > 0 && (
          <div className={styles.pagination}>
            <span>Trang {page}/{totalPages} · {policies.length} chính sách</span>
            <div>
              <Button size="sm" variant="secondary" disabled={page === 1} onClick={() => setPage((value) => value - 1)}>
                Trước
              </Button>
              <Button size="sm" variant="secondary" disabled={page === totalPages} onClick={() => setPage((value) => value + 1)}>
                Sau
              </Button>
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
