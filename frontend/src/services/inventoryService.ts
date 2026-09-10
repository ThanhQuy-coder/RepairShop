import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type { InventoryItem, InventoryTransactionItem } from '../types/inventory.types';

export const inventoryService = {
  getInventory: () => apiClient.get<InventoryItem[]>('/inventory').then((res) => res.data),

  createTransaction: (payload: {
    partId: string;
    type: 'Import' | 'Adjustment';
    quantity: number;
  }) => apiClient.post('/inventory/transactions', payload),

  getTransactions: (params: {
    partId?: string;
    type?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient
      .get<PagedResponse<InventoryTransactionItem>>('/inventory/transactions', { params })
      .then((res) => res.data),
};
