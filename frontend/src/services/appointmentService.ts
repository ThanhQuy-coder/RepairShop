import apiClient from './apiClient';

export interface AvailableSlot {
  timeSlotId: string;
  slotStart: string;
  slotEnd: string;
  maxCapacity: number;
  booked: number;
  available: number;
  isFull: boolean;
}

export interface AppointmentResponse {
  id: string;
  customerId: string;
  fullName: string;
  phone: string;
  appointmentDate: string;
  timeSlotId: string;
  slotStart: string;
  slotEnd: string;
  status: string;
  createdAt: string;
  lookupToken?: string;
}

export interface AppointmentDetailResponse extends AppointmentResponse {
  deviceType?: string;
  brand?: string;
  model?: string;
  serialNumber?: string;
  issueDescription?: string;
  rejectionReason?: string;
}

export const appointmentService = {
  getForStaff: () =>
    apiClient.get<AppointmentResponse[]>('/appointments/staff').then((res) => res.data),

  getMine: () => apiClient.get<AppointmentResponse[]>('/appointments/mine').then((res) => res.data),

  getById: (id: string) =>
    apiClient.get<AppointmentDetailResponse>(`/appointments/${id}`).then((res) => res.data),

  lookup: (lookupToken: string, phone: string) =>
    apiClient
      .post<AppointmentResponse>('/appointments/lookup', { lookupToken, phone })
      .then((res) => res.data),

  cancel: (id: string) =>
    apiClient.patch<AppointmentResponse>(`/appointments/${id}/cancel`).then((res) => res.data),

  confirm: (id: string) =>
    apiClient.patch<AppointmentResponse>(`/appointments/${id}/confirm`).then((res) => res.data),

  reject: (id: string, rejectionReason: string) =>
    apiClient
      .patch<AppointmentResponse>(`/appointments/${id}/reject`, { rejectionReason })
      .then((res) => res.data),

  convertToTicket: (id: string, customerId: string, deviceId: string) =>
    apiClient
      .post(`/appointments/${id}/convert-to-ticket`, { customerId, deviceId })
      .then((res) => res.data),

  getAvailableSlots: (date: string) =>
    apiClient
      .get<AvailableSlot[]>('/appointments/available-slots', { params: { date } })
      .then((res) => res.data),

  create: (payload: {
    fullName: string;
    phone: string;
    deviceInfo?: { deviceType?: string; brand?: string; model?: string; issueDescription?: string };
    appointmentDate: string;
    timeSlotId: string;
    customerId?: string | null;
  }) => apiClient.post<AppointmentResponse>('/appointments', payload).then((res) => res.data),

  noShow: (id: string) =>
    apiClient.patch<AppointmentResponse>(`/appointments/${id}/no-show`).then((res) => res.data),
};
