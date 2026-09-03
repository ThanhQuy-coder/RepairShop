import { useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function QaPassModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [form, setForm] = useState({
    functionalCheckNotes: '',
    cosmeticCheckNotes: '',
    originalIssueResolvedNotes: '',
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  const handleStartThenSubmit = async () => {
    if (Object.values(form).some((v) => !v.trim())) {
      setErrorMessage('Vui lòng điền đủ 3 mục kiểm tra.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.passQualityCheck(ticketId, form);
      showSuccess('QA đạt — thiết bị sẵn sàng bàn giao.');
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const field = (key: keyof typeof form, label: string) => (
    <div style={{ marginBottom: 12 }}>
      <label style={{ fontSize: 13, fontWeight: 500, display: 'block', marginBottom: 4 }}>
        {label}
      </label>
      <textarea
        rows={2}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        value={form[key]}
        onChange={(e) => setForm({ ...form, [key]: e.target.value })}
      />
    </div>
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Kiểm thử QA — Đạt"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button onClick={handleStartThenSubmit} isLoading={isSaving}>
            Xác nhận Đạt
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {field('functionalCheckNotes', 'Kiểm tra chức năng thiết bị *')}
      {field('cosmeticCheckNotes', 'Kiểm tra tình trạng ngoại hình *')}
      {field('originalIssueResolvedNotes', 'Xác nhận lỗi ban đầu đã khắc phục *')}
    </Modal>
  );
}
