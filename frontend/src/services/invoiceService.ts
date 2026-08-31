import apiClient from './apiClient';

export const invoiceService = {
  pay: (invoiceId: string) => apiClient.patch(`/invoices/${invoiceId}/pay`, {}),
};
