import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { UserListItem } from '../types/user.types';
import type { UserRole } from '../types/auth.types';

export const userService = {
  list: (params: { role?: string; isActive?: boolean; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResponse<UserListItem>>('/users', { params }).then((res) => res.data),
  create: (payload: {
    fullName: string;
    email: string;
    phone?: string;
    role: UserRole;
    password: string;
  }) => apiClient.post<UserListItem>('/users', payload).then((res) => res.data),
  setStatus: (id: string, isActive: boolean) =>
    apiClient.patch<UserListItem>(`/users/${id}/status`, { isActive }).then((res) => res.data),
};
