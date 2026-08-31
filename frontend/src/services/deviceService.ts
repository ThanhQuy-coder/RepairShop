import apiClient from './apiClient';
import type { Device, CreateDeviceRequest } from '../types/device.types';

export const deviceService = {
  getById: (id: string) => apiClient.get<Device>(`/devices/${id}`).then((res) => res.data),

  getByCustomerId: (customerId: string) =>
    apiClient.get<Device[]>(`/devices/by-customer/${customerId}`).then((res) => res.data),

  create: (payload: CreateDeviceRequest) =>
    apiClient.post<Device>('/devices', payload).then((res) => res.data),

  update: (id: string, payload: Partial<CreateDeviceRequest>) =>
    apiClient.put<Device>(`/devices/${id}`, { id, ...payload }).then((res) => res.data),
};
