import { useState } from 'react';
import { Button, ErrorMessage, ConfirmDialog } from '../common';
import { quoteService } from '../../services/quoteService';
import { extractApiError } from '../../utils/apiError';
import styles from './QuoteApprovalForm.module.css';
import { useToast } from '../../hooks/useToast';

interface QuoteApprovalFormProps {
  quoteId: string;
  onDecided: () => void; // báo cho page cha reload lại dữ liệu, KHÔNG tự set status ở đây
}

/**
 * Component này KHÔNG có bất kỳ input nào cho phép người dùng chọn "status mong muốn".
 * Chỉ có đúng 2 hành động gọi thẳng business API: quoteService.approve() / quoteService.reject().
 * Không có PUT/PATCH generic nào nhận { status: "..." } tùy ý — đúng yêu cầu:
 * "Frontend không được cho phép user tùy ý sửa status".
 */
export default function QuoteApprovalForm({ quoteId, onDecided }: QuoteApprovalFormProps) {
  const [rejectReason, setRejectReason] = useState('');
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [showApproveConfirm, setShowApproveConfirm] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { showSuccess } = useToast();

  const handleApprove = async () => {
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      await quoteService.approve(quoteId); // gọi ĐÚNG business API — Approve Quote

      showSuccess('Đã đồng ý báo giá. Cửa hàng sẽ tiến hành sửa chữa.');
      onDecided();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSubmitting(false);
      setShowApproveConfirm(false);
    }
  };

  const handleReject = async () => {
    if (!rejectReason.trim()) {
      setErrorMessage('Vui lòng nhập lý do từ chối.');
      return;
    }
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      await quoteService.reject(quoteId, rejectReason); // gọi ĐÚNG business API — Reject Quote

      showSuccess('Đã từ chối báo giá.');
      onDecided();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (showRejectForm) {
    return (
      <div>
        {errorMessage && <ErrorMessage message={errorMessage} />}
        <label className={styles.label}>Lý do từ chối *</label>
        <textarea
          className={styles.textarea}
          rows={3}
          value={rejectReason}
          onChange={(e) => setRejectReason(e.target.value)}
          placeholder="VD: Giá quá cao so với thị trường, tôi muốn mang đi nơi khác..."
        />
        <div className={styles.buttonRow}>
          <Button
            variant="secondary"
            onClick={() => setShowRejectForm(false)}
            disabled={isSubmitting}
          >
            Quay lại
          </Button>
          <Button variant="danger" onClick={handleReject} isLoading={isSubmitting}>
            Xác nhận từ chối
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <div className={styles.buttonRow}>
        <Button variant="danger" onClick={() => setShowRejectForm(true)}>
          Từ chối
        </Button>
        <Button onClick={() => setShowApproveConfirm(true)}>Đồng ý báo giá</Button>
      </div>

      <ConfirmDialog
        isOpen={showApproveConfirm}
        title="Xác nhận đồng ý báo giá"
        message="Sau khi đồng ý, cửa hàng sẽ bắt đầu tiến hành sửa chữa theo báo giá này. Bạn chắc chắn chứ?"
        confirmLabel="Đồng ý"
        isLoading={isSubmitting}
        onConfirm={handleApprove}
        onCancel={() => setShowApproveConfirm(false)}
      />
    </div>
  );
}
