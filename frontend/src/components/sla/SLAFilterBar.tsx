import { Button, Select } from '../common';
import type { SelectOption } from '../common';
import styles from './sla.module.css';

export interface SlaFilters { slaStatus: string; statusCode: string; deviceType: string }

const statusOptions: SelectOption[] = [
  { value: '', label: 'Tất cả SLA' }, { value: 'OnTrack', label: 'Đúng hạn' },
  { value: 'DueSoon', label: 'Sắp trễ' }, { value: 'Overdue', label: 'Quá hạn' },
];
const deviceOptions: SelectOption[] = [
  { value: '', label: 'Tất cả thiết bị' }, { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' }, { value: 'Tablet', label: 'Máy tính bảng' },
  { value: 'Electronics', label: 'Điện tử khác' }, { value: 'Smartwatch', label: 'Đồng hồ thông minh' },
];

export default function SLAFilterBar({ filters, onChange, onClear }: {
  filters: SlaFilters; onChange: (filters: SlaFilters) => void; onClear: () => void;
}) {
  return <div className={styles.filters}>
    <Select label="Trạng thái SLA" options={statusOptions} value={filters.slaStatus}
      onChange={(event) => onChange({ ...filters, slaStatus: event.target.value })} />
    <Select label="Loại thiết bị" options={deviceOptions} value={filters.deviceType}
      onChange={(event) => onChange({ ...filters, deviceType: event.target.value })} />
    <input aria-label="Mã bước workflow" placeholder="Bước workflow (ví dụ DIAGNOSING)" value={filters.statusCode}
      onChange={(event) => onChange({ ...filters, statusCode: event.target.value.toUpperCase() })} />
    <Button size="sm" variant="secondary" onClick={onClear}>Xóa lọc</Button>
  </div>;
}
