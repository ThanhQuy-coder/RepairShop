import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { ArticleDetail, ArticleListItem, ServiceItem } from '../types/content.types';

export interface UpsertServicePayload {
  name: string;
  description: string | null;
  basePrice: number | null;
  deviceType: string | null;
  imageUrl?: string | null;
  imageFile?: File | null;
  removeImage?: boolean;
}

export const contentService = {
  getPublicServices: async (params?: {
    page?: number;
    pageSize?: number;
    deviceType?: string;
    search?: string;
  }) => {
    const res = await apiClient.get<PagedResponse<ServiceItem>>('/services', { params });
    return res.data;
  },

  getServiceById: async (id: string) => {
    const res = await apiClient.get<ServiceItem>(`/services/${id}`);
    return res.data;
  },

  getAdminServices: async (params?: {
    page?: number;
    pageSize?: number;
    isActive?: boolean;
    deviceType?: string;
    search?: string;
  }) => {
    const res = await apiClient.get<PagedResponse<ServiceItem>>('/services/admin', { params });
    return res.data;
  },

  uploadServiceImage: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post<{ url: string; publicId: string }>('/services/upload-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },

  createService: async (payload: UpsertServicePayload) => {
    if (payload.imageFile) {
      const formData = new FormData();
      formData.append('name', payload.name);
      if (payload.description) formData.append('description', payload.description);
      if (payload.basePrice !== null && payload.basePrice !== undefined) {
        formData.append('basePrice', String(payload.basePrice));
      }
      if (payload.deviceType) formData.append('deviceType', payload.deviceType);
      if (payload.imageUrl) formData.append('imageUrl', payload.imageUrl);
      formData.append('image', payload.imageFile);

      const res = await apiClient.post<ServiceItem>('/services', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return res.data;
    }

    const res = await apiClient.post<ServiceItem>('/services', {
      name: payload.name,
      description: payload.description,
      basePrice: payload.basePrice,
      deviceType: payload.deviceType,
      imageUrl: payload.imageUrl ?? null,
    });
    return res.data;
  },

  updateService: async (id: string, payload: UpsertServicePayload) => {
    if (payload.imageFile) {
      const formData = new FormData();
      formData.append('name', payload.name);
      if (payload.description) formData.append('description', payload.description);
      if (payload.basePrice !== null && payload.basePrice !== undefined) {
        formData.append('basePrice', String(payload.basePrice));
      }
      if (payload.deviceType) formData.append('deviceType', payload.deviceType);
      if (payload.imageUrl !== undefined && payload.imageUrl !== null) {
        formData.append('imageUrl', payload.imageUrl);
      }
      if (payload.removeImage) {
        formData.append('removeImage', 'true');
      }
      formData.append('image', payload.imageFile);

      const res = await apiClient.put<ServiceItem>(`/services/${id}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return res.data;
    }

    const res = await apiClient.put<ServiceItem>(`/services/${id}`, {
      name: payload.name,
      description: payload.description,
      basePrice: payload.basePrice,
      deviceType: payload.deviceType,
      imageUrl: payload.imageUrl,
      removeImage: payload.removeImage ?? false,
    });
    return res.data;
  },

  toggleServicePublish: async (id: string, publish: boolean) => {
    await apiClient.patch(`/services/${id}/publish`, { publish });
  },

  // Articles
  getPublicArticles: async () => {
    const res = await apiClient.get<PagedResponse<ArticleListItem>>('/articles');
    return res.data;
  },
  getAdminArticles: async () => {
    const res = await apiClient.get<PagedResponse<ArticleListItem>>('/articles/admin');
    return res.data;
  },
  getArticleById: async (id: string) => {
    const res = await apiClient.get<ArticleDetail>(`/articles/${id}`);
    return res.data;
  },
  createArticle: async (payload: { title: string; content: string; imageUrl: string | null }) => {
    const res = await apiClient.post<ArticleDetail>('/articles', payload);
    return res.data;
  },
  updateArticle: async (
    id: string,
    payload: { title: string; content: string; imageUrl: string | null }
  ) => {
    const res = await apiClient.put<ArticleDetail>(`/articles/${id}`, payload);
    return res.data;
  },
  toggleArticlePublish: async (id: string, publish: boolean) => {
    await apiClient.patch(`/articles/${id}/publish`, { publish });
  },
};
