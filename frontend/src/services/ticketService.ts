import apiClient from './apiClient';
import type {
  Ticket,
  CreateTicketRequest,
  StatusHistoryItem,
  PublicTicketTracking,
} from '../types/ticket.types';
import type { PagedResponse } from '../types/common.types';
import type { TicketListItem, TicketListFilters } from '../types/ticket.types';

export const ticketService = {
  getById: (id: string) => apiClient.get<Ticket>(`/tickets/${id}`).then((res) => res.data),

  // Lưu ý: Backend chưa có endpoint "GET /customers/{id}/tickets" (bị bỏ qua có chủ đích ở Task 10
  // Tuần 3 vì mentor yêu cầu "chưa làm RepairTicket"). Gọi tạm để dành, xem ghi chú cuối bài.
  getByCustomerId: (customerId: string) =>
    apiClient.get<Ticket[]>(`/customers/${customerId}/tickets`).then((res) => res.data),

  create: (payload: CreateTicketRequest) =>
    apiClient.post<Ticket>('/tickets', payload).then((res) => res.data),

  assignTechnician: (ticketId: string, technicianId: string, note?: string) =>
    apiClient
      .patch<Ticket>(`/tickets/${ticketId}/assign-technician`, { technicianId, note })
      .then((res) => res.data),

  uploadImage: (ticketId: string, formData: FormData) =>
    apiClient
      .post(`/tickets/${ticketId}/images`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((res) => res.data),

  getStatusHistory: (ticketId: string) =>
    apiClient
      .get<StatusHistoryItem[]>(`/tickets/${ticketId}/status-history`)
      .then((res) => res.data),

  trackByCode: (ticketCode: string) =>
    apiClient
      .get<PublicTicketTracking>(`/public/tickets/${ticketCode}/tracking`)
      .then((res) => res.data),

  list: (filters: TicketListFilters) =>
    apiClient
      .get<PagedResponse<TicketListItem>>('/tickets', { params: filters })
      .then((res) => res.data),
};
