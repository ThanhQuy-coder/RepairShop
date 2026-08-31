import apiClient from './apiClient';
import type { Quote, CreateQuoteRequest } from '../types/quote.types';

export const quoteService = {
  getByTicketId: (ticketId: string) =>
    apiClient.get<Quote[]>(`/tickets/${ticketId}/quotes`).then((res) => res.data),

  create: (ticketId: string, payload: CreateQuoteRequest) =>
    apiClient.post<Quote>(`/tickets/${ticketId}/quotes`, payload).then((res) => res.data),

  approve: (quoteId: string) =>
    apiClient.patch<Quote>(`/quotes/${quoteId}/approve`).then((res) => res.data),

  reject: (quoteId: string, rejectReason: string) =>
    apiClient.patch<Quote>(`/quotes/${quoteId}/reject`, { rejectReason }).then((res) => res.data),
};
