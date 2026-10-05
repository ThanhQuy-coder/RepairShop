import apiClient from './apiClient';
import type { NotificationListResponse } from '../types/notification.types';

export const notificationService = {
  list: (params: { page?: number; pageSize?: number }) =>
    apiClient.get<NotificationListResponse>('/notifications', { params }).then((res) => res.data),

  getUnreadCount: () =>
    apiClient.get<{ count: number }>('/notifications/unread-count').then((res) => res.data.count),

  markAsRead: (id: string) => apiClient.patch(`/notifications/${id}/read`),

  markAllAsRead: () => apiClient.patch('/notifications/read-all'),
};
