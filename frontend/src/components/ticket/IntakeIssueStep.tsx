import { type FormEvent, useState } from 'react';
import { Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { Customer } from '../../types/customer.types';
import type { Device } from '../../types/device.types';
import type { Ticket } from '../../types/ticket.types';
import styles from './IntakeStep.module.css';

interface IntakeIssueStepProps {
  customer: Customer;
  device: Device;
  onBack: () => void;
  onCreated: (ticket: Ticket) => void;
}

export default function IntakeIssueStep({
  customer,
  device,
  onBack,
  onCreated,
}: IntakeIssueStepProps) {
  const [issueReported, setIssueReported] = useState('');
  const [conditionNotes, setConditionNotes] = useState('');
  const [riskWarning, setRiskWarning] = useState('');
  const [diagnosticDeposit, setDiagnosticDeposit] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (!issueReported.trim()) {
      setErrorMessage('Vui lòng nhập mô tả lỗi khách hàng khai báo.');
      return;
    }

    setIsSaving(true);
    try {
      const ticket = await ticketService.create({
        customerId: customer.id,
        deviceId: device.id,
        issueDescription: issueReported,
        conditionNotes: conditionNotes || undefined,
        riskWarning: riskWarning || undefined,
        diagnosticDeposit: diagnosticDeposit ? Number(diagnosticDeposit) : undefined,
      });
      onCreated(ticket);
    } catch (err) {
      const apiError = extractApiError(err);
      setErrorMessage(apiError.message);
      setErrors(apiError.errors);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <div className={styles.selectedCard}>
        <span>
          <strong>{customer.fullName}</strong> — {device.brand} {device.model}
        </span>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} errors={errors} />}

      <div className={styles.field}>
        <label className={styles.label}>Mô tả lỗi khách hàng khai báo *</label>
        <textarea
          className={styles.textarea}
          rows={3}
          value={issueReported}
          onChange={(e) => setIssueReported(e.target.value)}
          placeholder="VD: Máy nóng, pin tụt nhanh, thỉnh thoảng tự tắt nguồn..."
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label}>Tình trạng ban đầu (vết trầy/móp có sẵn)</label>
        <textarea
          className={styles.textarea}
          rows={2}
          value={conditionNotes}
          onChange={(e) => setConditionNotes(e.target.value)}
          placeholder="VD: Trầy nhẹ góc trên bên phải, màn hình còn nguyên vẹn..."
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label}>Cảnh báo rủi ro (nếu có)</label>
        <textarea
          className={styles.textarea}
          rows={2}
          value={riskWarning}
          onChange={(e) => setRiskWarning(e.target.value)}
          placeholder="VD: Máy vô nước, có thể không sửa được, không hứa trước với khách..."
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label}>Tiền cọc chẩn đoán (nếu thu)</label>
        <input
          type="number"
          min={0}
          className={styles.numberInput}
          value={diagnosticDeposit}
          onChange={(e) => setDiagnosticDeposit(e.target.value)}
          placeholder="0"
        />
      </div>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onBack} disabled={isSaving}>
          ← Quay lại
        </Button>
        <Button type="submit" isLoading={isSaving}>
          Tạo phiếu sửa chữa
        </Button>
      </div>
    </form>
  );
}
