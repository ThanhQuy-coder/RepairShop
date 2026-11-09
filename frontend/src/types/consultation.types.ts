export interface ConsultationPossibleCause {
  name: string;
  confidence: 'HIGH' | 'MEDIUM' | 'LOW' | string;
}

export interface ConsultationServiceItem {
  serviceId: string;
  serviceName: string;
  confidence: string;
}

export interface ConsultationPartItem {
  partId: string;
  partName: string;
  confidence: string;
}

export interface ConsultationDiagnosis {
  possibleCauses: ConsultationPossibleCause[];
  suggestedServices: ConsultationServiceItem[];
  suggestedParts: ConsultationPartItem[];
  missingInformation: string[];
  explanation: string;
}

export interface ConsultationQuotationItem {
  itemId: string;
  itemType: 'SERVICE' | 'PART' | string;
  name: string;
  unitPrice: number;
  lineTotal: number;
}

export interface ConsultationQuotation {
  items: ConsultationQuotationItem[];
  estimatedTotal: number;
  currency: string;
  assumptions: string[];
  explanation: string;
}

export interface ConsultationSlot {
  timeSlotId: string;
  slotStart: string;
  slotEnd: string;
  appointmentDate: string;
  available: number;
}

export interface ConsultationBookingDraft {
  fullName: string | null;
  phone: string | null;
  appointmentDate: string | null;
  timeSlotId: string | null;
  deviceType: string;
  brand: string;
  model: string;
  issueDescription: string | null;
}

export interface ConsultationAppointment {
  suggestedSlots: ConsultationSlot[];
  bookingDraft: ConsultationBookingDraft | null;
  missingInformation: string[];
  requiresUserConfirmation: boolean;
  explanation: string;
}

export interface ConsultationResponse {
  aiAvailable: boolean;
  status: 'SUCCESS' | 'PARTIAL' | 'NO_MATCH' | 'OUT_OF_SCOPE' | 'ERROR' | string;
  intent: string;
  executedAgents: string[];
  diagnosis: ConsultationDiagnosis | null;
  quotation: ConsultationQuotation | null;
  appointment: ConsultationAppointment | null;
  message: string;
  disclaimer: string;
  aiRequestId: string | null;
}

export interface ConsultationRequest {
  deviceType: string;
  brand: string;
  model: string;
  issueDescription: string;
  requestedDate?: string;
  allowQuotation?: boolean;
  allowAppointment?: boolean;
  fullName?: string;
  phone?: string;
}

export interface CreateAppointmentRequest {
  fullName: string;
  phone: string;
  deviceInfo: {
    deviceType: string;
    brand: string;
    model: string;
    issueDescription?: string;
  };
  appointmentDate: string;
  timeSlotId: string;
  type?: string;
}

export interface CreateAppointmentResponse {
  id: string;
  status: string;
  appointmentDate: string;
  lookupToken?: string | null;
}
