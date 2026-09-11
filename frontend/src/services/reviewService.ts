import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { ReviewListItem } from '../types/review.types';

export const reviewService = {
  getPublicReviews: () =>
    apiClient.get<PagedResponse<ReviewListItem>>('/reviews').then((res) => res.data),
  getAdminReviews: () =>
    apiClient.get<PagedResponse<ReviewListItem>>('/reviews/admin').then((res) => res.data),
  toggleVisibility: (id: string, isVisible: boolean) =>
    apiClient.patch(`/reviews/${id}/visibility`, { isVisible }),
  createReview: (ticketId: string, rating: number, comment?: string) =>
    apiClient.post(`/tickets/${ticketId}/review`, { rating, comment }),
};
