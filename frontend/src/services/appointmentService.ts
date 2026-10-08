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
  fullName: string;
  phone: string;
  appointmentDate: string;
  timeSlotId: string;
  status: string;
  createdAt: string;
}

export const appointmentService = {
  getAvailableSlots: (date: string) =>
    apiClient.get<AvailableSlot[]>('/appointments/available-slots', { params: { date } }).then((res) => res.data),
  create: (payload: {
    fullName: string;
    phone: string;
    deviceInfo?: { deviceType?: string; brand?: string; model?: string; issueDescription?: string };
    appointmentDate: string;
    timeSlotId: string;
    customerId?: string | null;
  }) => apiClient.post<AppointmentResponse>('/appointments', payload).then((res) => res.data),
};
