import apiClient from './apiClient';
import type { DashboardSummary, RevenueReport, TechnicianSummaryItem } from '../types/reports.types';

export const reportsService = {
  getDashboardSummary: () =>
    apiClient.get<DashboardSummary>('/reports/dashboard-summary').then((res) => res.data),

  getRevenueReport: (params: { fromDate?: string; toDate?: string; groupBy?: 'day' | 'month' }) =>
    apiClient.get<RevenueReport>('/reports/revenue', { params }).then((res) => res.data),

  getTechnicianPerformance: (params: { fromDate?: string; toDate?: string }) =>
    apiClient
      .get<TechnicianSummaryItem[]>('/reports/technician-performance', { params })
      .then((res) => res.data),
};
