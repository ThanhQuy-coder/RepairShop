import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { useAuth } from '../../hooks/useAuth';
import { Badge, Button, ErrorMessage, Loading } from '../../components/common';
import { TICKET_STATUS_BADGE_VARIANT, TICKET_STATUS_LABELS } from '../../constants/ticketStatus';
import styles from './CustomerHomePage.module.css';

export default function CustomerHomePage() {
  const navigate = useNavigate();
  const { email } = useAuth();
  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    ticketService
      .list({ page: 1, pageSize: 50 })
      .then((result) => setTickets(result.items))
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  }, []);

  const activeTickets = tickets.filter(
    (ticket) => !['DELIVERED', 'CLOSED_REJECTED'].includes(ticket.status)
  );
  const readyTickets = tickets.filter((ticket) => ticket.status === 'READY_FOR_PICKUP');
  const firstName = email?.split('@')[0] || 'bạn';

  return (
    <div className={styles.container}>
      <section className={styles.welcomePanel}>
        <div>
          <span className={styles.eyebrow}>KHU VỰC KHÁCH HÀNG</span>
          <h1 className={styles.title}>Chào mừng trở lại, {firstName}</h1>
          <p className={styles.subtitle}>
            Theo dõi thiết bị, tiến độ sửa chữa và thông tin bảo hành của bạn.
          </p>
        </div>
        <div className={styles.welcomeMark}>RS</div>
      </section>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}

      {!isLoading && !errorMessage && (
        <>
          <section className={styles.statsGrid} aria-label="Tổng quan phiếu sửa chữa">
            <div className={styles.statCard}>
              <span className={styles.statIcon}>◷</span>
              <div>
                <strong>{activeTickets.length}</strong>
                <span>Đang xử lý</span>
              </div>
            </div>
            <div className={`${styles.statCard} ${styles.highlight}`}>
              <span className={styles.statIcon}>✓</span>
              <div>
                <strong>{readyTickets.length}</strong>
                <span>Sẵn sàng nhận</span>
              </div>
            </div>
            <div className={styles.statCard}>
              <span className={styles.statIcon}>▣</span>
              <div>
                <strong>{tickets.length}</strong>
                <span>Tổng phiếu</span>
              </div>
            </div>
          </section>

          <section className={styles.contentGrid}>
            <div className={styles.section}>
              <div className={styles.sectionHeader}>
                <div>
                  <span className={styles.sectionKicker}>CẬP NHẬT GẦN ĐÂY</span>
                  <h2>Phiếu sửa chữa của tôi</h2>
                </div>
                <Button variant="ghost" size="sm" onClick={() => navigate('/customer/my-tickets')}>
                  Xem tất cả →
                </Button>
              </div>
              {tickets.length === 0 ? (
                <div className={styles.emptyState}>
                  <span>◌</span>
                  <p>Bạn chưa có phiếu sửa chữa nào.</p>
                  <Button size="sm" onClick={() => navigate('/track')}>
                    Tra cứu phiếu
                  </Button>
                </div>
              ) : (
                <div className={styles.ticketList}>
                  {tickets.slice(0, 4).map((ticket) => (
                    <button
                      type="button"
                      className={styles.ticketRow}
                      key={ticket.id}
                      onClick={() => navigate(`/tickets/${ticket.id}`)}
                    >
                      <span className={styles.deviceIcon}>⌁</span>
                      <span className={styles.ticketInfo}>
                        <strong>{ticket.ticketCode}</strong>
                        <span>{ticket.deviceLabel}</span>
                      </span>
                      <span className={styles.ticketStatus}>
                        <Badge variant={TICKET_STATUS_BADGE_VARIANT[ticket.status]}>
                          {TICKET_STATUS_LABELS[ticket.status]}
                        </Badge>
                        <small>{new Date(ticket.receivedAt).toLocaleDateString('vi-VN')}</small>
                      </span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            <aside className={styles.quickPanel}>
              <span className={styles.sectionKicker}>THAO TÁC NHANH</span>
              <h2>Cần hỗ trợ?</h2>
              <p>
                Tra cứu tiến độ bằng mã phiếu hoặc xem chính sách bảo hành cho thiết bị của bạn.
              </p>
              <div className={styles.quickActions}>
                <Button onClick={() => navigate('/track')}>Tra cứu tiến độ</Button>
                <Button variant="secondary" onClick={() => navigate('/customer/warranty')}>
                  Xem bảo hành
                </Button>
              </div>
            </aside>
          </section>
        </>
      )}
    </div>
  );
}
