import { useCallback, useEffect, useRef, useState } from 'react';
import { reportsService } from '../services/reportsService';
import { notificationSignalRService } from '../services/notificationSignalRService';
import { extractApiError } from '../utils/apiError';
import type { SlaAnalytics, SlaMyTicketsResponse, SlaTicketListResponse } from '../types/reports.types';
import type { SlaFilters } from '../components/sla';

const EMPTY_FILTERS: SlaFilters = { slaStatus: '', statusCode: '', deviceType: '' };

export function useAdminSlaDashboard() {
  const [data, setData] = useState<SlaAnalytics | null>(null);
  const [tickets, setTickets] = useState<SlaTicketListResponse | null>(null);
  const [filters, setFilters] = useState(EMPTY_FILTERS);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const lastUpdated = useRef<Date | null>(null);

  const fetchData = useCallback(async (background = false) => {
    background ? setRefreshing(true) : setLoading(true);
    try {
      const [analytics, ticketData] = await Promise.all([
        reportsService.getSlaAnalytics(),
        reportsService.getSlaTickets({ ...filters, page, pageSize: 10 }),
      ]);
      setData(analytics); setTickets(ticketData); setError(null); lastUpdated.current = new Date();
    } catch (e) { setError(extractApiError(e).message); }
    finally { setLoading(false); setRefreshing(false); }
  }, [filters, page]);

  useEffect(() => { void fetchData(); }, [fetchData]);
  useEffect(() => {
    const timer = window.setInterval(() => void fetchData(true), 60000);
    const unsubscribe = notificationSignalRService.subscribeToNotifications((type) => {
      if (type?.toLowerCase() === 'slaoverdue') void fetchData(true);
    });
    return () => { window.clearInterval(timer); unsubscribe(); };
  }, [fetchData]);
  return { data, tickets, filters, setFilters: (next: SlaFilters) => { setFilters(next); setPage(1); }, page, setPage, loading, refreshing, error, lastUpdated: lastUpdated.current, retry: () => fetchData() };
}

export function useStaffSlaDashboard() {
  const [data, setData] = useState<SlaTicketListResponse | null>(null);
  const [filters, setFiltersState] = useState(EMPTY_FILTERS);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const fetchData = useCallback(async (background = false) => {
    background ? setRefreshing(true) : setLoading(true);
    try { setData(await reportsService.getSlaTickets({ ...filters, page, pageSize: 10 })); setError(null); }
    catch (e) { setError(extractApiError(e).message); }
    finally { setLoading(false); setRefreshing(false); }
  }, [filters, page]);
  useEffect(() => { void fetchData(); }, [fetchData]);
  useEffect(() => {
    const timer = window.setInterval(() => void fetchData(true), 60000);
    const unsubscribe = notificationSignalRService.subscribeToNotifications((type) => {
      if (type?.toLowerCase() === 'slaoverdue') void fetchData(true);
    });
    return () => { window.clearInterval(timer); unsubscribe(); };
  }, [fetchData]);
  return { data, filters, setFilters: (next: SlaFilters) => { setFiltersState(next); setPage(1); }, page, setPage, loading, refreshing, error, retry: () => fetchData() };
}

export function useTechnicianSlaDashboard() {
  const [data, setData] = useState<SlaMyTicketsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const fetchData = useCallback(async (background = false) => {
    background ? setRefreshing(true) : setLoading(true);
    try { setData(await reportsService.getMySlaTickets()); setError(null); }
    catch (e) { setError(extractApiError(e).message); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);
  useEffect(() => { void fetchData(); const timer = window.setInterval(() => void fetchData(true), 60000); const unsubscribe = notificationSignalRService.subscribeToNotifications((type) => { if (type?.toLowerCase() === 'slaoverdue') void fetchData(true); }); return () => { window.clearInterval(timer); unsubscribe(); }; }, [fetchData]);
  return { data, loading, refreshing, error, retry: () => fetchData() };
}
