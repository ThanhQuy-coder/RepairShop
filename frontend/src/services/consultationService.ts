import apiClient from './apiClient';
import type {
  ConsultationRequest,
  ConsultationResponse,
  CreateAppointmentRequest,
  CreateAppointmentResponse,
} from '../types/consultation.types';

export const consultationService = {
  consult: (payload: ConsultationRequest) =>
    apiClient.post<ConsultationResponse>('/ai/consult', payload).then((res) => res.data),

  /**
   * Đặt lịch CHỈ sau khi người dùng xác nhận. Backend tạo Appointment ở trạng thái Pending
   * và kiểm tra capacity/khóa slot — AI không tạo lịch hẹn.
   */
  book: (payload: CreateAppointmentRequest) =>
    apiClient.post<CreateAppointmentResponse>('/appointments', payload).then((res) => res.data),
};
