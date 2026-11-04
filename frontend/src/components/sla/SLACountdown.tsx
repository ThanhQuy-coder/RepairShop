import { useEffect, useState } from 'react';
import { normalizeSlaStatus } from './SLAStatusBadge';
import type { SlaStatus } from '../../types/reports.types';

function formatDuration(minutes: number) {
  const absolute = Math.max(0, Math.round(Math.abs(minutes)));
  const hours = Math.floor(absolute / 60);
  const mins = absolute % 60;
  return hours > 0 ? `${hours}g ${mins}p` : `${mins}p`;
}

export default function SLACountdown({ dueAt, slaStatus }: { dueAt: string; slaStatus: SlaStatus | number }) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 60000);
    return () => window.clearInterval(timer);
  }, []);
  const minutes = (new Date(dueAt).getTime() - now) / 60000;
  const overdue = normalizeSlaStatus(slaStatus) === 'Overdue';
  return <span aria-label={overdue ? `Trễ ${formatDuration(minutes)}` : `Còn ${formatDuration(minutes)}`}>
    {overdue ? `Trễ ${formatDuration(minutes)}` : `Còn ${formatDuration(minutes)}`}
  </span>;
}
