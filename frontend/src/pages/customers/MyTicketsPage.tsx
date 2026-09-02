import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { Loading, ErrorMessage, EmptyState, Badge } from '../../components/common';
import { TICKET_STATUS_LABELS, TICKET_STATUS_BADGE_VARIANT } from '../../constants/ticketStatus';
import styles from './MyTicketsPage.module.css';

export default function MyTicketsPage() {
  const navigate = useNavigate();
  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    // Backend tự lọc theo Customer đang đăng nhập (mở rộng tương tự ownership filter của
    // Technician ở Task 5.10 — cần Backend bổ sung "if role == Customer thì lọc CustomerId
    // theo UserId" trong RepairTicketRepository.SearchAsync; xem ghi chú review cuối bài).
    ticketService
      .list({ pageSize: 50 })
      .then((res) => setTickets(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} />;
  if (tickets.length === 0) return <EmptyState message="Bạn chưa có phiếu sửa chữa nào." />;

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Phiếu sửa chữa của tôi</h2>
      <div className={styles.list}>
        {tickets.map((t) => (
          <div key={t.id} className={styles.card} onClick={() => navigate(`/tickets/${t.id}`)}>
            <div>
              <strong>{t.ticketCode}</strong>
              <p className={styles.device}>{t.deviceLabel}</p>
            </div>
            <Badge variant={TICKET_STATUS_BADGE_VARIANT[t.status]}>
              {TICKET_STATUS_LABELS[t.status]}
            </Badge>
          </div>
        ))}
      </div>
    </div>
  );
}
