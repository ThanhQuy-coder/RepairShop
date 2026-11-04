import { normalizeSlaStatus } from './SLAStatusBadge';
import type { SlaStatus } from '../../types/reports.types';

export default function SLAProgressBar({ startedAt, dueAt, slaStatus }: {
  startedAt: string; dueAt: string; slaStatus: SlaStatus | number;
}) {
  const start = new Date(startedAt).getTime();
  const due = new Date(dueAt).getTime();
  const value = due <= start ? 100 : Math.min(100, Math.max(0, ((Date.now() - start) / (due - start)) * 100));
  const status = normalizeSlaStatus(slaStatus).toLowerCase();
  return <div className={`sla-progress sla-progress-${status}`} role="progressbar"
    aria-label={`Đã sử dụng ${Math.round(value)}% thời gian SLA`} aria-valuenow={Math.round(value)} aria-valuemin={0} aria-valuemax={100}>
    <span style={{ width: `${value}%` }} />
  </div>;
}
