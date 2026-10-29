import { useEffect, useState } from 'react';
import { ticketService } from '../services/ticketService';
import { extractApiError } from '../utils/apiError';
import { DASHBOARD_STATUS_GROUPS } from '../constants/ticketStatus';

export interface TicketSummary {
  total: number;
  groups: { key: string; label: string; icon: string; count: number }[];
}

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
