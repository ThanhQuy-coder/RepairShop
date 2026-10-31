export type InspectionType = 'Functional' | 'Appearance' | 'RepairVerification';

export interface InspectionTemplate {
  id: string;
  name: string;
  deviceType: string | null;
  inspectionType: InspectionType;
  sortOrder: number;
}

export interface InspectionItem {
  id?: string;
  templateId?: string | null;
  itemName: string;
  status: string;
  note?: string | null;
}

export interface InspectionResponse {
  ticketId: string;
  inspectionType: InspectionType;
  items: InspectionItem[];
}
