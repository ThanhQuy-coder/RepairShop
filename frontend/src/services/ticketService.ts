import apiClient from './apiClient';
import type {
  Ticket,
  CreateTicketRequest,
  StatusHistoryItem,
  PublicTicketTracking,
  TicketDetail,
  TicketPartUsed,
  TicketImage,
} from '../types/ticket.types';
import type { PagedResponse } from '../types/common.types';
import type { TicketListItem, TicketListFilters } from '../types/ticket.types';

export const ticketService = {
  getById: (id: string) => apiClient.get<TicketDetail>(`/tickets/${id}`).then((res) => res.data),

  // Lưu ý: Backend chưa có endpoint "GET /customers/{id}/tickets" (bị bỏ qua có chủ đích ở Task 10
  // Tuần 3 vì mentor yêu cầu "chưa làm RepairTicket"). Gọi tạm để dành, xem ghi chú cuối bài.
  getByCustomerId: (customerId: string) =>
    apiClient.get<TicketListItem[]>(`/customers/${customerId}/tickets`).then((res) => res.data),

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

  getImages: (ticketId: string) =>
    apiClient.get<TicketImage[]>(`/tickets/${ticketId}/images`).then((res) => res.data),

  startDiagnosis: (ticketId: string) =>
    apiClient.patch<TicketDetail>(`/tickets/${ticketId}/start-diagnosis`).then((res) => res.data),

  submitDiagnosis: (
    ticketId: string,
    payload: {
      diagnosisResult: string;
      rootCause?: string;
      recommendedRepair?: string;
      requiredPartsNote?: string;
      technicalNote?: string;
    }
  ) =>
    apiClient
      .patch<TicketDetail>(`/tickets/${ticketId}/diagnosis`, payload)
      .then((res) => res.data),

  addRepairNote: (ticketId: string, note: string) =>
    apiClient.post(`/tickets/${ticketId}/repair-notes`, { note }),

  usePart: (ticketId: string, partId: string, quantity: number) =>
    apiClient
      .post<TicketPartUsed>(`/tickets/${ticketId}/parts`, { partId, quantity })
      .then((res) => res.data),

  recordCompletionNotes: (ticketId: string, completionNotes: string) =>
    apiClient.patch(`/tickets/${ticketId}/completion-notes`, { completionNotes }),

  startQualityCheck: (ticketId: string) =>
    apiClient.patch<TicketDetail>(`/tickets/${ticketId}/start-qa`).then((res) => res.data),

  passQualityCheck: (
    ticketId: string,
    payload: {
      functionalCheckNotes: string;
      cosmeticCheckNotes: string;
      originalIssueResolvedNotes: string;
    }
  ) =>
    apiClient.patch<TicketDetail>(`/tickets/${ticketId}/qa-pass`, payload).then((res) => res.data),

  failQualityCheck: (ticketId: string, failureReason: string) =>
    apiClient
      .patch<TicketDetail>(`/tickets/${ticketId}/qa-fail`, { failureReason })
      .then((res) => res.data),

  createInvoice: (ticketId: string, paymentMethod: 'Cash' | 'Transfer') =>
    apiClient.post(`/tickets/${ticketId}/invoice`, { paymentMethod }),

  deliver: (ticketId: string, deliveryNote?: string) =>
    apiClient
      .patch<TicketDetail>(`/tickets/${ticketId}/deliver`, { deliveryNote })
      .then((res) => res.data),

  createWarranty: (ticketId: string, warrantyMonths: number, terms?: string) =>
    apiClient.post(`/tickets/${ticketId}/warranty`, { warrantyMonths, terms }),
};
