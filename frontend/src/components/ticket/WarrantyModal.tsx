import { useState } from 'react';
import { Modal, Button, Input, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function WarrantyModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [months, setMonths] = useState(6);
  const [terms, setTerms] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  const handleSubmit = async () => {
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.createWarranty(ticketId, months, terms || undefined);
      showSuccess('Đã tạo thông tin bảo hành.');
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
      title="Tạo thông tin bảo hành"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Tạo bảo hành
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <Input
        label="Thời hạn bảo hành (tháng)"
        type="number"
        min={1}
        max={36}
        value={months}
        onChange={(e) => setMonths(Number(e.target.value))}
      />
      <div style={{ marginTop: 12 }}>
        <label style={{ fontSize: 13, fontWeight: 500, display: 'block', marginBottom: 4 }}>
          Điều khoản bảo hành
        </label>
        <textarea
          rows={2}
          style={{
            width: '100%',
            padding: 10,
            border: '1px solid var(--color-border)',
            borderRadius: 4,
          }}
          value={terms}
          onChange={(e) => setTerms(e.target.value)}
        />
      </div>
    </Modal>
  );
}
