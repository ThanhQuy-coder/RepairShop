import apiClient from './apiClient';
import type { WarrantyPackage } from '../types/warrantyPackage.types';

export const warrantyPackageService = {
  listActive: () => apiClient.get<WarrantyPackage[]>('/warranty-packages').then((r) => r.data),
  listAdmin: () => apiClient.get<WarrantyPackage[]>('/warranty-packages/admin').then((r) => r.data),
  create: (payload: Omit<WarrantyPackage, 'id' | 'isActive'>) =>
    apiClient.post<WarrantyPackage>('/warranty-packages', payload).then((r) => r.data),
  update: (id: string, payload: Omit<WarrantyPackage, 'id' | 'isActive'>) =>
    apiClient.put<WarrantyPackage>(`/warranty-packages/${id}`, payload).then((r) => r.data),
  toggle: (id: string, isActive: boolean) =>
    apiClient.patch(`/warranty-packages/${id}/active`, { isActive }),
  remove: (id: string) => apiClient.delete(`/warranty-packages/${id}`),
};
