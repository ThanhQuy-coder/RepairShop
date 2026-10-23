import { useEffect, useState } from 'react';
import { appointmentService } from '../services/appointmentService';

export interface AppointmentSummaryGroup {
  key: string;
  icon: string;
  label: string;
  count: number;
}

export interface AppointmentSummary {
  total: number;
  groups: AppointmentSummaryGroup[];
}

// Suy ra từ các action: confirm / reject / cancel / noShow / markCompleted
// (mặc định khi vừa tạo sẽ là "pending")
// Nếu backend dùng casing/format khác, chỉ cần chỉnh key ở đây.
const STATUS_META: Record<string, { icon: string; label: string; order: number }> = {
  pending: { icon: '⏳', label: 'Chờ xác nhận', order: 1 },
  confirmed: { icon: '✅', label: 'Đã xác nhận', order: 2 },
  completed: { icon: '🎉', label: 'Hoàn thành', order: 3 },
  rejected: { icon: '⛔', label: 'Đã từ chối', order: 4 },
  cancelled: { icon: '🚫', label: 'Đã hủy', order: 5 },
  no_show: { icon: '❓', label: 'Không đến', order: 6 },
};

function normalizeStatus(status: string | null | undefined): string {
  return (status ?? '')
    .toString()
    .trim()
    .toLowerCase()
    .replace(/[\s-]+/g, '_');
}

export function useAppointmentSummary() {
  const [summary, setSummary] = useState<AppointmentSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setErrorMessage(null);

    appointmentService
      .getForStaff()
      .then((appointments) => {
        if (cancelled) return;
        const counts = appointments.reduce<Record<string, number>>((acc, a) => {
          const key = normalizeStatus(a.status) || 'unknown';
          acc[key] = (acc[key] ?? 0) + 1;
          return acc;
        }, {});
        const groups: AppointmentSummaryGroup[] = Object.entries(counts)
          .map(([key, count]) => {
            const meta = STATUS_META[key] ?? { icon: '📌', label: key, order: 99 };
            return { key, icon: meta.icon, label: meta.label, count, order: meta.order };
          })
          .sort((a, b) => a.order - b.order)
          .map(({ order: _order, ...rest }) => rest);
        setSummary({ total: appointments.length, groups });
      })
      .catch((err) => {
        if (cancelled) return;
        setErrorMessage(err?.response?.data?.message ?? 'Không thể tải thống kê lịch hẹn.');
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return { summary, isLoading, errorMessage };
}
