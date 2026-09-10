import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { Part, CreatePartRequest } from '../types/inventory.types';

export const partService = {
  list: (params: { search?: string; category?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResponse<Part>>('/parts', { params }).then((res) => res.data),

  getById: (id: string) => apiClient.get<Part>(`/parts/${id}`).then((res) => res.data),

  create: (payload: CreatePartRequest) =>
    apiClient.post<Part>('/parts', payload).then((res) => res.data),

  update: (id: string, payload: Omit<CreatePartRequest, 'sku'>) =>
    apiClient.put<Part>(`/parts/${id}`, payload).then((res) => res.data),
};
