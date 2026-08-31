import { useState } from 'react';
import type { Quote } from '../../types/quote.types';
import { Badge, Button } from '../common';
import CreateQuoteModal from './CreateQuoteModal';
import styles from './TicketQuoteSection.module.css';

interface TicketQuoteSectionProps {
  ticketId: string;
  ticketStatus: string;
  quotes: Quote[];
  canCreateQuote: boolean; // true khi role Receptionist/Admin và status = DIAGNOSING
  onQuoteCreated: () => void;
}

const QUOTE_STATUS_VARIANT: Record<string, 'default' | 'success' | 'danger'> = {
  Pending: 'default',
  Approved: 'success',
  Rejected: 'danger',
};

export default function TicketQuoteSection({
  ticketId,
  ticketStatus,
  quotes,
  canCreateQuote,
  onQuoteCreated,
}: TicketQuoteSectionProps) {
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <div>
      {quotes.length === 0 ? (
        <p className={styles.empty}>Chưa có báo giá nào.</p>
      ) : (
        <div className={styles.list}>
          {quotes.map((q) => (
            <div key={q.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span>{q.description}</span>
                <Badge variant={QUOTE_STATUS_VARIANT[q.status]}>{q.status}</Badge>
              </div>
              <ul className={styles.items}>
                {q.items.map((item) => (
                  <li key={item.id}>
                    {item.description} × {item.quantity} — {item.subtotal.toLocaleString('vi-VN')}đ
                  </li>
                ))}
              </ul>
              <div className={styles.total}>
                Tổng: <strong>{q.totalAmount.toLocaleString('vi-VN')}đ</strong>
              </div>
            </div>
          ))}
        </div>
      )}

      {canCreateQuote && ticketStatus === 'DIAGNOSING' && (
        <Button size="sm" onClick={() => setIsModalOpen(true)}>
          + Tạo báo giá
        </Button>
      )}

      <CreateQuoteModal
        isOpen={isModalOpen}
        ticketId={ticketId}
        onClose={() => setIsModalOpen(false)}
        onCreated={() => {
          setIsModalOpen(false);
          onQuoteCreated();
        }}
      />
    </div>
  );
}
