import apiClient from './apiClient';
import type { DashboardSummary } from '../types/reports.types';

export const reportsService = {
  getDashboardSummary: () =>
    apiClient.get<DashboardSummary>('/reports/dashboard-summary').then((res) => res.data),
};
