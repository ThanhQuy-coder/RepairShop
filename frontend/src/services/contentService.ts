import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { ServiceItem, ArticleListItem, ArticleDetail } from '../types/content.types';

export const contentService = {
  getPublicServices: () =>
    apiClient.get<PagedResponse<ServiceItem>>('/services').then((res) => res.data),
  getAdminServices: () =>
    apiClient.get<PagedResponse<ServiceItem>>('/services/admin').then((res) => res.data),
  createService: (payload: {
    name: string;
    description?: string;
    basePrice?: number;
    deviceType?: string;
  }) => apiClient.post('/services', payload),
  updateService: (
    id: string,
    payload: { name: string; description?: string; basePrice?: number; deviceType?: string }
  ) => apiClient.put(`/services/${id}`, payload),
  toggleServicePublish: (id: string, publish: boolean) =>
    apiClient.patch(`/services/${id}/publish`, { publish }),

  getPublicArticles: () =>
    apiClient.get<PagedResponse<ArticleListItem>>('/articles').then((res) => res.data),
  getAdminArticles: () =>
    apiClient.get<PagedResponse<ArticleListItem>>('/articles/admin').then((res) => res.data),
  getArticleById: (id: string) =>
    apiClient.get<ArticleDetail>(`/articles/${id}`).then((res) => res.data),
  createArticle: (payload: { title: string; content: string; imageUrl?: string }) =>
    apiClient.post('/articles', payload),
  updateArticle: (id: string, payload: { title: string; content: string; imageUrl?: string }) =>
    apiClient.put(`/articles/${id}`, payload),
  toggleArticlePublish: (id: string, publish: boolean) =>
    apiClient.patch(`/articles/${id}/publish`, { publish }),
};
