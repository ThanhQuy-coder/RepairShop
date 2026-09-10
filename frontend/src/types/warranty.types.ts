export interface Warranty {
  warrantyCode: string;
  ticketId: string;
  startDate: string;
  endDate: string;
  terms: string | null;
  status: 'Active' | 'Voided';
  isExpired: boolean;
}

export interface MyWarrantyItem {
  warrantyCode: string;
  ticketId: string;
  ticketCode: string;
  deviceLabel: string;
  startDate: string;
  endDate: string;
  status: 'Active' | 'Voided';
  isExpired: boolean;
}
