export type QuoteItemType = 'Service' | 'Part';
export type QuoteStatus = 'Pending' | 'Approved' | 'Rejected' | 'QuoteRejected' | 'NeedsRequote';

export interface QuoteItem {
  id: string;
  itemType: QuoteItemType;
  partId: string | null;
  serviceId: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Quote {
  id: string;
  ticketId: string;
  description: string;
  totalAmount: number;
  status: QuoteStatus;
  items: QuoteItem[];
  createdAt: string;
  version: number;
  previousQuoteId?: string;
}

export interface CreateQuoteRequest {
  description: string;
  items: {
    itemType: QuoteItemType;
    quantity: number;
    partId?: string;
    serviceId?: string;
  }[];
}
