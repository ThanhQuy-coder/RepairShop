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
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>BÀN LÀM VIỆC KỸ THUẬT</span>
          <h1 className={styles.title}>Phiếu sửa chữa được phân công</h1>
          <p className={styles.subtitle}>
            Theo dõi tiến trình kiểm tra, chẩn đoán, thay thế linh kiện và kiểm định chất lượng QA.
          </p>
        </div>
      </div>

      {summary && (
        <div className={styles.summaryGrid}>
          <SummaryCard icon="📋" label="Tổng số việc" value={summary.total} isEmphasized />
          {summary.groups
            .filter((g) => g.count > 0 || g.key !== 'closed')
            .map((g) => (
              <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
            ))}
        </div>
      )}

      <div className={styles.groupContainer}>
        {GROUPS.map((statusCode) => {
          const items = tickets.filter((t) => t.status === statusCode);
          return (
            <section key={statusCode} className={styles.group}>
              <div className={styles.groupHeader}>
                <div className={styles.groupTitleWrap}>
                  <Badge variant={TICKET_STATUS_BADGE_VARIANT[statusCode]}>
                    {TICKET_STATUS_LABELS[statusCode]}
                  </Badge>
                  <span className={styles.countPill}>{items.length}</span>
                </div>
              </div>

              {items.length === 0 ? (
                <div className={styles.emptyGroupBox}>
                  <span>Không có phiếu trong trạng thái này</span>
                </div>
              ) : (
                <div className={styles.cardGrid}>
                  {items.map((t) => (
                    <div
                      key={t.id}
                      className={styles.card}
                      onClick={() => navigate(`/tickets/${t.id}`)}
                    >
                      <div className={styles.cardHeader}>
                        <strong className={styles.ticketCode}>{t.ticketCode}</strong>
                        <span className={styles.openCue}>Chi tiết →</span>
                      </div>
                      <div className={styles.customerLine}>
                        <span className={styles.customerIcon}>👤</span>
                        <span className={styles.customerName}>{t.customerName}</span>
                      </div>
                      <div className={styles.deviceLine}>
                        <span className={styles.deviceBadge}>{t.deviceLabel}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>
          );
        })}
      </div>

      {tickets.length === 0 && <EmptyState message="Bạn chưa được phân công ticket nào." />}
    </div>
  );
}

