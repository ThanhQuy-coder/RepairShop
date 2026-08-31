import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '../types/customer.types';

export const customerService = {
  list: (params: { search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResponse<Customer>>('/customers', { params }).then((res) => res.data),

  getById: (id: string) =>
    apiClient.get<Customer & { devices?: unknown[] }>(`/customers/${id}`).then((res) => res.data),

  create: (payload: CreateCustomerRequest) =>
    apiClient.post<Customer>('/customers', payload).then((res) => res.data),

  update: (payload: UpdateCustomerRequest) =>
    apiClient.put<Customer>(`/customers/${payload.id}`, payload).then((res) => res.data),
};
