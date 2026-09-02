import { useState } from 'react';
import type { Quote } from '../../types/quote.types';
import { Button } from '../common';
import QuoteCard from '../quote/QuoteCard';
import CreateQuoteModal from '../ticket/CreateQuoteModal';

interface TicketQuoteSectionProps {
  ticketId: string;
  ticketStatus: string;
  quotes: Quote[];
  canCreateQuote: boolean;
  onQuoteCreated: () => void;
}

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
        <p style={{ color: 'var(--color-text-muted)', fontSize: 14, marginBottom: 12 }}>
          Chưa có báo giá nào.
        </p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 12 }}>
          {quotes.map((q) => (
            <QuoteCard key={q.id} quote={q} />
            // Receptionist/Admin/Technician chỉ XEM (không truyền children) — Approve/Reject là hành động
            // riêng của Customer, đặt ở QuotePendingPage (mục 5 bên dưới), KHÔNG lộ ở màn hình nhân viên.
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
