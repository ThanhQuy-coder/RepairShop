import apiClient from './apiClient';
import type {
  DashboardSummary,
  RevenueReport,
  SlaSummary,
  ProfitReport,
  TechnicianSummaryItem,
  SlaAnalytics,
  SlaMyTicketsResponse,
  SlaTicketListResponse,
} from '../types/reports.types';

export interface SlaPolicy {
  id: string;
  statusCode: string;
  deviceType: number | null;
  durationMinutes: number;
  isActive: boolean;
}

export const reportsService = {
  getDashboardSummary: () =>
    apiClient.get<DashboardSummary>('/reports/dashboard-summary').then((res) => res.data),

  getRevenueReport: (params: { fromDate?: string; toDate?: string; groupBy?: 'day' | 'month' }) =>
    apiClient.get<RevenueReport>('/reports/revenue', { params }).then((res) => res.data),

  getTechnicianPerformance: (params: { fromDate?: string; toDate?: string }) =>
    apiClient
      .get<TechnicianSummaryItem[]>('/reports/technician-performance', { params })
      .then((res) => res.data),

  getSlaSummary: () =>
    apiClient.get<SlaSummary>('/reports/sla-summary').then((res) => res.data),

  getSlaAnalytics: () =>
    apiClient.get<SlaAnalytics>('/reports/sla-analytics').then((res) => res.data),

  getSlaTickets: (params: {
    slaStatus?: string;
    statusCode?: string;
    deviceType?: string;
    page: number;
    pageSize: number;
  }) => apiClient.get<SlaTicketListResponse>('/sla/tickets', { params }).then((res) => res.data),

  getMySlaTickets: () =>
    apiClient.get<SlaMyTicketsResponse>('/sla/my-tickets').then((res) => res.data),

  getSlaPolicies: () =>
    apiClient.get<SlaPolicy[]>('/sla/policies').then((res) => res.data),

  createSlaPolicy: (payload: {
    statusCode: string;
    deviceType: string | null;
    durationMinutes: number;
  }) => apiClient.post<SlaPolicy>('/sla/policies', payload).then((res) => res.data),

  updateSlaPolicy: (id: string, payload: { durationMinutes: number; isActive: boolean }) =>
    apiClient.put(`/sla/policies/${id}`, payload),

  getProfitReport: (params: { fromDate?: string; toDate?: string }) =>
    apiClient.get<ProfitReport>('/reports/profit', { params }).then((res) => res.data),
};
