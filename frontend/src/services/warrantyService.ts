import apiClient from './apiClient';
import type { MyWarrantyItem } from '../types/warranty.types';
import type { AppointmentResponse, AvailableSlot } from './appointmentService';

export const warrantyService = {
  getMyWarranties: () => apiClient.get<MyWarrantyItem[]>('/warranty/my').then((res) => res.data),

  createClaim: (ticketId: string, issueReported: string, appointmentDate: string, timeSlotId: string) =>
    apiClient.post<AppointmentResponse>(`/tickets/${ticketId}/warranty-claim`, {
      issueReported,
      appointmentDate,
      timeSlotId,
    }).then((res) => res.data),
  getAvailableSlots: (date: string) =>
    apiClient.get<AvailableSlot[]>('/appointments/available-slots', { params: { date } }).then((res) => res.data),
  searchForStaff: (params: { phone?: string; warrantyCode?: string }) =>
    apiClient.get<StaffWarrantyLookup[]>('/warranty/staff/search', { params }).then((res) => res.data),
};

export interface StaffWarrantyLookup {
  warrantyCode: string;
  ticketId: string;
  ticketCode: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  deviceId: string;
  deviceLabel: string;
  startDate: string;
  endDate: string;
  status: string;
}
