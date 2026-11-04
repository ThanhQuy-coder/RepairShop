import { Badge } from '../common';
import type { SlaStatus } from '../../types/reports.types';

const statusMap: Record<string, { label: string; icon: string; variant: 'success' | 'warning' | 'danger' | 'default' }> = {
  OnTrack: { label: 'Đúng hạn', icon: '✓', variant: 'success' },
  DueSoon: { label: 'Sắp trễ', icon: '⏱', variant: 'warning' },
  Overdue: { label: 'Quá hạn', icon: '!', variant: 'danger' },
  Completed: { label: 'Hoàn tất', icon: '✓', variant: 'default' },
};

export function normalizeSlaStatus(status: SlaStatus | number): keyof typeof statusMap {
  if (typeof status === 'number') return ['OnTrack', 'DueSoon', 'Overdue', 'Completed'][status] as keyof typeof statusMap;
  const key = status.replaceAll('_', '').toLowerCase();
  return key === 'ontrack' ? 'OnTrack' : key === 'duesoon' ? 'DueSoon' : key === 'overdue' ? 'Overdue' : 'Completed';
}

export default function SLAStatusBadge({ status }: { status: SlaStatus | number }) {
  const item = statusMap[normalizeSlaStatus(status)];
  return <Badge variant={item.variant}>{item.icon} {item.label}</Badge>;
}
