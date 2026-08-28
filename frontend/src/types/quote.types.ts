export type QuoteItemType = "Service" | "Part";
export type QuoteStatus = "Pending" | "Approved" | "Rejected";

export interface QuoteItem {
  id: string;
  itemType: QuoteItemType;
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
}

export interface CreateQuoteRequest {
  description: string;
  items: {
    itemType: QuoteItemType;
    description: string;
    quantity: number;
    unitPrice: number;
    partId?: string;
  }[];
}
