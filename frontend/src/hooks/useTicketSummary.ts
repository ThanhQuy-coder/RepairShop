import { useEffect, useState } from 'react';
import { ticketService } from '../services/ticketService';
import { extractApiError } from '../utils/apiError';
import { DASHBOARD_STATUS_GROUPS } from '../constants/ticketStatus';

export interface TicketSummary {
  total: number;
  groups: { key: string; label: string; icon: string; count: number }[];
}

/**
 * Tính summary từ danh sách ticket thật (GET /api/tickets) — KHÔNG gọi API Reports
 * (chưa tồn tại, thuộc Tuần 7). Đây là giải pháp tạm thời có chủ đích cho Dashboard tối thiểu
 * Tuần 5, sẽ thay bằng GetTicketsSummaryQuery thật khi Tuần 7 hoàn thiện Reports module.
 */
export function useTicketSummary() {
  const [summary, setSummary] = useState<TicketSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        // pageSize lớn để lấy toàn bộ ticket trong 1 lần — chấp nhận được ở quy mô đồ án (NFR-003:
        // ~50 người dùng đồng thời), Tuần 7 sẽ thay bằng API COUNT thật phía Backend, hiệu quả hơn
        // nhiều so với kéo hết dữ liệu về đếm tay như cách tạm này.
        const res = await ticketService.list({ pageSize: 500 });
        if (cancelled) return;

        const groups = DASHBOARD_STATUS_GROUPS.map((g) => ({
          key: g.key,
          label: g.label,
          icon: g.icon,
          count: res.items.filter((t) => g.statuses.includes(t.status)).length,
        }));

        setSummary({ total: res.total, groups });
      } catch (err) {
        if (!cancelled) setErrorMessage(extractApiError(err).message);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, []);

  return { summary, isLoading, errorMessage };
}
