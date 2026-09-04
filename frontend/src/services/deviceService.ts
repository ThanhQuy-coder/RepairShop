import apiClient from './apiClient';
import type { Device, CreateDeviceRequest } from '../types/device.types';
import type { TicketListItem } from '../types/ticket.types';

export const deviceService = {
  getById: (id: string) => apiClient.get<Device>(`/devices/${id}`).then((res) => res.data),

  getByCustomerId: (customerId: string) =>
    apiClient.get<Device[]>(`/devices/by-customer/${customerId}`).then((res) => res.data),

  getRepairHistory: (id: string) =>
    apiClient
      .get<{ items: TicketListItem[] }>(`/devices/${id}/repair-history`)
      .then((res) => res.data.items),

  create: (payload: CreateDeviceRequest) =>
    apiClient.post<Device>('/devices', payload).then((res) => res.data),

  update: (id: string, payload: Omit<CreateDeviceRequest, 'customerId' | 'deviceType'>) =>
    apiClient.put<Device>(`/devices/${id}`, { id, ...payload }).then((res) => res.data),
};
