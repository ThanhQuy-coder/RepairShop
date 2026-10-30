import apiClient from './apiClient';
import type { DiagnosisSymptom, DeviceType } from '../types/diagnosis.types';

export const diagnosisService = {
  list: (deviceType: DeviceType) =>
    apiClient
      .get<{ items: DiagnosisSymptom[] }>('/diagnosis-symptoms', { params: { deviceType } })
      .then((res) => res.data.items),
  listAdmin: () =>
    apiClient
      .get<{ items: DiagnosisSymptom[] }>('/diagnosis-symptoms/admin')
      .then((res) => res.data.items),
  create: (payload: { name: string; deviceType: DeviceType; sortOrder: number }) =>
    apiClient.post<DiagnosisSymptom>('/diagnosis-symptoms', payload).then((res) => res.data),
  update: (id: string, payload: { name: string; deviceType: DeviceType; sortOrder: number }) =>
    apiClient.put<DiagnosisSymptom>(`/diagnosis-symptoms/${id}`, payload).then((res) => res.data),
  toggleActive: (id: string, isActive: boolean) =>
    apiClient.patch(`/diagnosis-symptoms/${id}/active`, { isActive }),
};
