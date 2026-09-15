export interface Part {
  id: string;
  name: string;
  sku: string;
  category: string | null;
  compatibleDeviceType: string | null;
  costPrice: number;
  unitPrice: number;
  unit: string;
  minStockThreshold: number;
  quantityOnHand: number;
  isLowStock: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePartRequest {
  name: string;
  sku: string;
  costPrice: number;
  unitPrice: number;
  category?: string;
  compatibleDeviceType?: string;
  unit: string;
  minStockThreshold: number;
}

export interface InventoryTransactionItem {
  id: string;
  partId: string;
  partName: string;
  type: 'Import' | 'Export' | 'IncreaseAdjustment' | 'DownwardAdjustment';
  quantity: number;
  relatedTicketId: string | null;
  performedByName: string;
  createdAt: string;
}

export interface InventoryItem {
  partId: string;
  partName: string;
  quantityOnHand: number;
  minStockThreshold: number;
  isLowStock: boolean;
}

export interface InventoryDashboardSummary {
  totalParts: number;
  lowStockCount: number;
  outOfStockCount: number;
}
