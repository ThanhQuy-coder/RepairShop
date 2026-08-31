import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { UserListItem } from '../types/user.types';

export const userService = {
  list: (params: { role?: string; isActive?: boolean; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResponse<UserListItem>>('/users', { params }).then((res) => res.data),
};
