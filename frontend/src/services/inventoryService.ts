import apiClient from './apiClient';
import type { PagedResponse } from '../types/common.types';
import type {
  InventoryDashboardSummary,
  InventoryItem,
  InventoryTransactionItem,
  Part,
} from '../types/inventory.types';

export const inventoryService = {
  getInventory: () => apiClient.get<InventoryItem[]>('/inventory').then((res) => res.data),

  createTransaction: (payload: {
    partId: string;
    type: 'Import' | 'IncreaseAdjustment' | 'DownwardAdjustment';
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

  getDashboard: () =>
    apiClient.get<InventoryDashboardSummary>('/inventory/dashboard').then((res) => res.data),

  getLowStockParts: (filter: 'All' | 'LowStock' | 'OutOfStock') =>
    apiClient.get<Part[]>('/inventory/low-stock', { params: { filter } }).then((res) => res.data),
};
