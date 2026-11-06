import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { ReviewListItem, ServiceReviewListResponse } from '../types/review.types';

export const reviewService = {
  getPublicReviews: async (page = 1, pageSize = 20) => {
    const res = await apiClient.get<PagedResponse<ReviewListItem>>('/reviews', {
      params: { page, pageSize },
    });
    return res.data;
  },

  getServiceReviews: async (serviceId: string, page = 1, pageSize = 6) => {
    const res = await apiClient.get<ServiceReviewListResponse>(`/services/${serviceId}/reviews`, {
      params: { page, pageSize },
    });
    return res.data;
  },

  getAdminReviews: async () => {
    const res = await apiClient.get<PagedResponse<ReviewListItem>>('/reviews/admin');
    return res.data;
  },

  toggleVisibility: async (id: string, isVisible: boolean) => {
    await apiClient.patch(`/reviews/${id}/visibility`, { isVisible });
  },

  createReview: async (ticketId: string, rating: number, comment?: string) => {
    const res = await apiClient.post(`/tickets/${ticketId}/review`, { rating, comment });
    return res.data;
  },
};
