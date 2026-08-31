import { useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function QaFailModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [reason, setReason] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async () => {
    if (!reason.trim()) {
      setErrorMessage('Vui lòng nhập lý do QA không đạt.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.startQualityCheck(ticketId).catch(() => null); // đã ở QA_TESTING thì bỏ qua lỗi này
      await ticketService.failQualityCheck(ticketId, reason);
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
      title="Kiểm thử QA — Không đạt"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button variant="danger" onClick={handleSubmit} isLoading={isSaving}>
            Xác nhận Không đạt
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <textarea
        rows={3}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        placeholder="VD: Màn hình bị ám vàng góc trên..."
        value={reason}
        onChange={(e) => setReason(e.target.value)}
      />
    </Modal>
  );
}
