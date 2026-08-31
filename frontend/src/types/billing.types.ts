export interface Invoice {
  id: string;
  ticketId: string;
  totalAmount: number;
  paymentMethod: 'Cash' | 'Transfer';
  paidAt: string | null;
  createdAt: string;
}
