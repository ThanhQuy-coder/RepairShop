import { useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function WarrantyClaimModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const { showSuccess } = useToast();
  const [issueReported, setIssueReported] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async () => {
    if (!issueReported.trim()) {
      setErrorMessage('Vui lòng mô tả vấn đề gặp phải.');
      return;
    }
    setErrorMessage(null);
    setIsSaving(true);
    try {
      await warrantyService.createClaim(ticketId, issueReported);
      showSuccess('Đã tạo yêu cầu bảo hành. Vui lòng mang thiết bị đến cửa hàng.');
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
      title="Yêu cầu bảo hành"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Gửi yêu cầu
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <label style={{ fontSize: 13, fontWeight: 500, display: 'block', marginBottom: 4 }}>
        Mô tả vấn đề gặp phải *
      </label>
      <textarea
        rows={3}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        value={issueReported}
        onChange={(e) => setIssueReported(e.target.value)}
        placeholder="VD: Máy lại bị tình trạng pin tụt nhanh như lần trước..."
      />
    </Modal>
  );
}
