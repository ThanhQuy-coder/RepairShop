import { useState } from 'react';
import { Modal, Button, Select, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { invoiceService } from '../../services/invoiceService';
import { extractApiError } from '../../utils/apiError';
import type { Quote } from '../../types/quote.types';

interface Props {
  isOpen: boolean;
  ticketId: string;
  approvedQuote?: Quote;
  onClose: () => void;
  onDone: () => void;
}

export default function DeliveryModal({ isOpen, ticketId, approvedQuote, onClose, onDone }: Props) {
  const [paymentMethod, setPaymentMethod] = useState<'Cash' | 'Transfer'>('Cash');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async () => {
    setIsSaving(true);
    setErrorMessage(null);
    try {
      // Payment = Mock/Manual (Task 4.12, Tuần 4) — xuất Invoice, đánh dấu đã thanh toán ngay tại quầy, rồi Deliver
      const invoiceRes = await ticketService.createInvoice(ticketId, paymentMethod);
      const invoiceId = (invoiceRes.data as { id: string }).id;
      await invoiceService.pay(invoiceId);
      await ticketService.deliver(ticketId);
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Xuất hóa đơn & Bàn giao"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Xác nhận bàn giao
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {approvedQuote && (
        <p style={{ marginBottom: 12 }}>
          Tổng tiền: <strong>{approvedQuote.totalAmount.toLocaleString('vi-VN')}đ</strong>
        </p>
      )}
      <Select
        label="Hình thức thanh toán"
        value={paymentMethod}
        options={[
          { value: 'Cash', label: 'Tiền mặt' },
          { value: 'Transfer', label: 'Chuyển khoản' },
        ]}
        onChange={(e) => setPaymentMethod(e.target.value as 'Cash' | 'Transfer')}
      />
    </Modal>
  );
}
