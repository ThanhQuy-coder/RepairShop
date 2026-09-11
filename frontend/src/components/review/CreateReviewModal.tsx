import { useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { reviewService } from '../../services/reviewService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function CreateReviewModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const { showSuccess } = useToast();
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async () => {
    setErrorMessage(null);
    setIsSaving(true);
    try {
      await reviewService.createReview(ticketId, rating, comment || undefined);
      showSuccess('Cảm ơn bạn đã đánh giá dịch vụ!');
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
      title="Đánh giá dịch vụ"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Gửi đánh giá
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}

      <div style={{ display: 'flex', gap: 4, marginBottom: 16, fontSize: 28 }}>
        {[1, 2, 3, 4, 5].map((star) => (
          <span
            key={star}
            style={{ cursor: 'pointer', color: star <= rating ? '#f59e0b' : '#d1d5db' }}
            onClick={() => setRating(star)}
          >
            ★
          </span>
        ))}
      </div>

      <textarea
        rows={3}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        value={comment}
        onChange={(e) => setComment(e.target.value)}
        placeholder="Chia sẻ trải nghiệm của bạn..."
      />
    </Modal>
  );
}
