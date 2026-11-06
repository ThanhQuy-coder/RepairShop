import apiClient from './apiClient';
import type {
  BrandItem,
  CreateDeviceRequest,
  Device,
  DeviceModelItem,
  UpsertBrandPayload,
  UpsertDeviceModelPayload,
} from '../types/device.types';
import type { TicketListItem } from '../types/ticket.types';
import type { PagedResponse } from '../types/common.types';

export const deviceService = {
  getById: async (id: string) => {
    const res = await apiClient.get<Device>(`/devices/${id}`);
    return res.data;
  },

  getByCustomerId: async (customerId: string) => {
    const res = await apiClient.get<Device[]>(`/devices/by-customer/${customerId}`);
    return res.data;
  },

  getRepairHistory: async (id: string) => {
    const res = await apiClient.get<{ items: TicketListItem[] }>(`/devices/${id}/repair-history`);
    return res.data.items;
  },

  create: async (payload: CreateDeviceRequest) => {
    const res = await apiClient.post<Device>('/devices', payload);
    return res.data;
  },

  update: async (
    id: string,
    payload: Omit<CreateDeviceRequest, 'customerId' | 'deviceType'>
  ) => {
    const res = await apiClient.put<Device>(`/devices/${id}`, { id, ...payload });
    return res.data;
  },

  // Brand Catalog APIs
  getPublicBrands: async (params?: { deviceType?: string; search?: string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<PagedResponse<BrandItem>>('/brands', { params });
    return res.data;
  },

  getAdminBrands: async (params?: {
    deviceType?: string;
    isActive?: boolean;
    search?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const res = await apiClient.get<PagedResponse<BrandItem>>('/brands/admin', { params });
    return res.data;
  },

  createBrand: async (payload: UpsertBrandPayload) => {
    const res = await apiClient.post<BrandItem>('/brands', payload);
    return res.data;
  },

  updateBrand: async (id: string, payload: UpsertBrandPayload) => {
    const res = await apiClient.put<BrandItem>(`/brands/${id}`, payload);
    return res.data;
  },

  toggleBrandActive: async (id: string, isActive: boolean) => {
    const res = await apiClient.patch<BrandItem>(`/brands/${id}/active`, { isActive });
    return res.data;
  },

  deleteBrand: async (id: string) => {
    await apiClient.delete(`/brands/${id}`);
  },

  // DeviceModel Catalog APIs
  getPublicModels: async (params?: {
    deviceType?: string;
    brandId?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const res = await apiClient.get<PagedResponse<DeviceModelItem>>('/device-models', { params });
    return res.data;
  },

  getAdminModels: async (params?: {
    deviceType?: string;
    brandId?: string;
    isActive?: boolean;
    search?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const res = await apiClient.get<PagedResponse<DeviceModelItem>>('/device-models/admin', { params });
    return res.data;
  },

  createDeviceModel: async (payload: UpsertDeviceModelPayload) => {
    const res = await apiClient.post<DeviceModelItem>('/device-models', payload);
    return res.data;
  },

  updateDeviceModel: async (id: string, payload: UpsertDeviceModelPayload) => {
    const res = await apiClient.put<DeviceModelItem>(`/device-models/${id}`, payload);
    return res.data;
  },

  toggleDeviceModelActive: async (id: string, isActive: boolean) => {
    const res = await apiClient.patch<DeviceModelItem>(`/device-models/${id}/active`, { isActive });
    return res.data;
  },

  deleteDeviceModel: async (id: string) => {
    await apiClient.delete(`/device-models/${id}`);
  },
};
