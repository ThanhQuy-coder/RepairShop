// Khớp RepairStatusCodes (Backend.Domain/Common, Tuần 4)
export type TicketStatus =
  | 'CHECKED_IN'
  | 'ASSIGNED'
  | 'DIAGNOSING'
  | 'WAITING_APPROVAL'
  | 'ON_HOLD'
  | 'WAITING_PARTS'
  | 'IN_REPAIR'
  | 'QA_TESTING'
  | 'READY_FOR_PICKUP'
  | 'DELIVERED'
  | 'CLOSED_REJECTED';

export interface Ticket {
  id: string;
  ticketCode: string;
  customerId: string;
  deviceId: string;
  status: TicketStatus;
  issueReported: string;
  notes?: string;
  conditionNotes?: string;
  riskWarning?: string;
  receivedAt: string;
}

export interface CreateTicketRequest {
  customerId: string;
  deviceId: string;
  issueDescription: string;
  notes?: string;
  conditionNotes?: string;
  riskWarning?: string;
  diagnosticDeposit?: number;
}

export interface StatusHistoryItem {
  ticketId: string;
  fromStatus: string | null;
  toStatus: string;
  changedByName: string;
  changedAt: string;
  note?: string;
}

export interface PublicTicketTracking {
  ticketCode: string;
  deviceLabel: string; // Backend trả "deviceLabel", không phải "device"
  status: string; // code kỹ thuật
  statusLabel: string; // nhãn tiếng Việt hiện sẵn — dùng trực tiếp, không cần map lại ở Frontend
  statusHistory: PublicStatusHistoryItem[];
  estimatedCompletion: string | null;
}

export interface TicketListItem {
  id: string;
  ticketCode: string;
  customerName: string;
  deviceLabel: string;
  technicianName: string | null;
  status: TicketStatus;
  receivedAt: string;
  issueReported?: string;
}

export interface TicketListFilters {
  status?: string;
  technicianId?: string;
  customerId?: string;
  page?: number;
  pageSize?: number;
}

export interface TicketDetail extends Ticket {
  customerName?: string;
  deviceLabel?: string;
  technicianName?: string | null;
  customerPhone?: string;
  deviceType?: string;
  deviceBrand?: string;
  deviceModel?: string;
  deviceSerialNumber?: string;
  images?: TicketImage[];
  usedParts?: TicketPartUsed[];
  invoice?: TicketInvoice;
  diagnosisResult?: string;
  rootCause?: string;
  recommendedRepair?: string;
  requiredPartsNote?: string;
  completionNotes?: string;
}

export interface TicketImage {
  id: string;
  imageUrl: string;
  imageType: 'BeforeRepair' | 'AfterRepair' | 'Other';
  uploadedAt: string;
}

export interface TicketPartUsed {
  ticketPartId: string;
  partName: string;
  quantity: number;
  unitPriceAtUse: number;
  subtotal: number;
}

export interface TicketInvoice {
  id: string;
  totalAmount: number;
  paymentMethod: 'Cash' | 'Transfer';
  paidAt: string | null;
  createdAt: string;
}

export interface PublicStatusHistoryItem {
  status: string; // code kỹ thuật, VD: "IN_REPAIR"
  statusLabel: string; // nhãn tiếng Việt, VD: "Đang sửa chữa"
  changedAt: string;
}
