export type DeviceType = 'Phone' | 'Laptop' | 'Electronics' | 'Tablet' | 'Smartwatch';

export interface DiagnosisSymptom {
  id: string;
  name: string;
  deviceType: DeviceType;
  isActive: boolean;
  sortOrder: number;
}
