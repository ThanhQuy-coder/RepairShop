export type DeviceType = 'Phone' | 'Laptop' | 'Tablet' | 'Smartwatch' | 'Electronics';

export const DEVICE_TYPE_OPTIONS: { value: DeviceType; label: string }[] = [
  { value: 'Phone', label: 'Điện thoại (Phone)' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Tablet', label: 'Máy tính bảng (Tablet)' },
  { value: 'Smartwatch', label: 'Đồng hồ thông minh (Smartwatch)' },
  { value: 'Electronics', label: 'Thiết bị điện tử (Electronics)' },
];

export const DEVICE_TYPE_LABELS: Record<string, string> = {
  Phone: 'Điện thoại',
  Laptop: 'Laptop',
  Tablet: 'Máy tính bảng',
  Smartwatch: 'Smartwatch',
  Electronics: 'Thiết bị điện tử',
};

export interface BrandItem {
  id: string;
  name: string;
  code: string;
  logoUrl: string | null;
  description: string | null;
  isActive: boolean;
  deviceTypes: string[];
  modelCount: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertBrandPayload {
  name: string;
  code: string;
  logoUrl?: string | null;
  description?: string | null;
  isActive?: boolean;
}

export interface DeviceModelItem {
  id: string;
  brandId: string;
  brandName: string;
  brandCode: string;
  deviceType: DeviceType;
  modelName: string;
  modelNumber: string | null;
  releaseYear: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertDeviceModelPayload {
  brandId: string;
  deviceType: DeviceType | string;
  modelName: string;
  modelNumber?: string | null;
  releaseYear?: number | null;
  isActive?: boolean;
}

export interface Device {
  id: string;
  customerId: string;
  deviceModelId?: string | null;
  deviceType: DeviceType;
  brand: string;
  model: string;
  serialNumber?: string;
  createdAt: string;
  isConvertTicket: boolean;
}

export interface CreateDeviceRequest {
  customerId: string;
  deviceType: DeviceType;
  brand: string;
  model: string;
  serialNumber?: string;
  deviceModelId?: string | null;
}

export interface DeviceRepairHistoryItem {
  ticketCode: string;
  issueReported: string;
  status: string;
  createdAt: string;
}
