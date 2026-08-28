// Khớp RepairStatusCodes (Backend.Domain/Common, Tuần 4)
export type TicketStatus =
  | "CHECKED_IN"
  | "ASSIGNED"
  | "DIAGNOSING"
  | "WAITING_APPROVAL"
  | "ON_HOLD"
  | "WAITING_PARTS"
  | "IN_REPAIR"
  | "QA_TESTING"
  | "READY_FOR_PICKUP"
  | "DELIVERED"
  | "CLOSED_REJECTED";

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
  deviceLabel: string;
  status: string;
  statusLabel: string;
  statusHistory: { status: string; statusLabel: string; changedAt: string }[];
  estimatedCompletion: string | null;
}
