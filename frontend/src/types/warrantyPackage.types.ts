export interface WarrantyPackage {
  id: string;
  name: string;
  description: string | null;
  durationMonths: number;
  terms: string;
  isActive: boolean;
}
