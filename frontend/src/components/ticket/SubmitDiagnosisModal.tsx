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

export default function SubmitDiagnosisModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [form, setForm] = useState({
    diagnosisResult: '',
    rootCause: '',
    recommendedRepair: '',
    requiredPartsNote: '',
    technicalNote: '',
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

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

  const handleSubmit = async () => {
    if (!form.diagnosisResult.trim()) {
      setErrorMessage('Vui lòng nhập kết quả chẩn đoán.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.submitDiagnosis(ticketId, form);
      showSuccess('Đã ghi nhận kết quả chẩn đoán.');
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
      title="Ghi kết quả chẩn đoán"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Lưu
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {field('diagnosisResult', 'Kết quả chẩn đoán *')}
      {field('rootCause', 'Nguyên nhân gốc')}
      {field('recommendedRepair', 'Đề xuất sửa chữa')}
      {field('requiredPartsNote', 'Linh kiện cần thiết')}
      {field('technicalNote', 'Ghi chú kỹ thuật')}
    </Modal>
  );
}
