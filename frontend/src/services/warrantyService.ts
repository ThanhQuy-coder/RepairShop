import apiClient from './apiClient';
import type { MyWarrantyItem } from '../types/warranty.types';

export const warrantyService = {
  getMyWarranties: () => apiClient.get<MyWarrantyItem[]>('/warranty/my').then((res) => res.data),

  createClaim: (ticketId: string, issueReported: string) =>
    apiClient.post(`/tickets/${ticketId}/warranty-claim`, { issueReported }),
};
