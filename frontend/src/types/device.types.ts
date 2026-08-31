export type DeviceType = 'Phone' | 'Laptop' | 'Electronics';

export interface Device {
  id: string;
  customerId: string;
  deviceType: DeviceType;
  brand: string;
  model: string;
  serialNumber?: string;
  createdAt: string;
}

export interface CreateDeviceRequest {
  customerId: string;
  deviceType: DeviceType;
  brand: string;
  model: string;
  serialNumber?: string;
}

export interface DeviceRepairHistoryItem {
  ticketCode: string;
  issueReported: string;
  status: string;
  createdAt: string;
}
