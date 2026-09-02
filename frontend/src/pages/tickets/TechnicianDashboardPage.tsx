import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { Badge, Loading, ErrorMessage, EmptyState } from '../../components/common';
import { TICKET_STATUS_LABELS, TICKET_STATUS_BADGE_VARIANT } from '../../constants/ticketStatus';
import styles from './TechnicianDashboardPage.module.css';
import { useTicketSummary } from '../../hooks/useTicketSummary';
import SummaryCard from '../../components/dashboard/SummaryCard';

// Backend đã tự lọc "chỉ ticket của Technician đang đăng nhập" ở tầng Repository (Task 5.10) —
// Frontend chỉ cần gọi list() bình thường, KHÔNG cần truyền technicianId thủ công.
const GROUPS = ['ASSIGNED', 'DIAGNOSING', 'WAITING_PARTS', 'IN_REPAIR', 'QA_TESTING'];

export default function TechnicianDashboardPage() {
  const navigate = useNavigate();
  const { summary } = useTicketSummary();
  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    ticketService
      .list({ pageSize: 200 })
      .then((res) => setTickets(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} />;

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Ticket của tôi</h2>

      {summary && (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
            gap: 12,
            marginBottom: 24,
          }}
        >
          <SummaryCard icon="📋" label="Tổng số" value={summary.total} isEmphasized />
          {summary.groups
            .filter((g) => g.count > 0 || g.key !== 'closed')
            .map((g) => (
              <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
            ))}
        </div>
      )}

      {GROUPS.map((statusCode) => {
        const items = tickets.filter((t) => t.status === statusCode);
        return (
          <section key={statusCode} className={styles.group}>
            <div className={styles.groupHeader}>
              <Badge variant={TICKET_STATUS_BADGE_VARIANT[statusCode]}>
                {TICKET_STATUS_LABELS[statusCode]}
              </Badge>
              <span className={styles.count}>({items.length})</span>
            </div>

            {items.length === 0 ? (
              <p className={styles.emptyText}>Không có ticket nào.</p>
            ) : (
              <div className={styles.cardGrid}>
                {items.map((t) => (
                  <div
                    key={t.id}
                    className={styles.card}
                    onClick={() => navigate(`/tickets/${t.id}`)}
                  >
                    <strong>{t.ticketCode}</strong>
                    <span>{t.customerName}</span>
                    <span className={styles.deviceText}>{t.deviceLabel}</span>
                  </div>
                ))}
              </div>
            )}
          </section>
        );
      })}

      {tickets.length === 0 && <EmptyState message="Bạn chưa được phân công ticket nào." />}
    </div>
  );
}
