export interface Warranty {
  warrantyCode: string;
  ticketId: string;
  startDate: string;
  endDate: string;
  terms: string | null;
  status: 'Active' | 'Voided';
  isExpired: boolean;
}
