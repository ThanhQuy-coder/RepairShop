import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { Loading, ErrorMessage, EmptyState, Badge, Button } from '../../components/common';
import {
  TICKET_STATUS_LABELS,
  TICKET_STATUS_BADGE_VARIANT,
  MAIN_TIMELINE_SEQUENCE,
  STATUS_ICON,
} from '../../constants/ticketStatus';
import styles from './MyTicketsPage.module.css';

function getTicketProgress(status: string): { percent: number; stepText: string } {
  if (status === 'CLOSED_REJECTED') {
    return { percent: 100, stepText: 'Đã đóng (Từ chối báo giá)' };
  }
  if (status === 'WAITING_PARTS') {
    return { percent: 55, stepText: 'Bước 5/8 · Đang chờ linh kiện thay thế' };
  }
  if (status === 'ON_HOLD') {
    return { percent: 45, stepText: 'Đang tạm hoãn chờ xử lý' };
  }
  const idx = MAIN_TIMELINE_SEQUENCE.indexOf(status);
  if (idx === -1) return { percent: 20, stepText: TICKET_STATUS_LABELS[status] ?? status };
  const step = idx + 1;
  const total = MAIN_TIMELINE_SEQUENCE.length;
  return {
    percent: Math.round((step / total) * 100),
    stepText: `Bước ${step}/${total} · ${TICKET_STATUS_LABELS[status] ?? status}`,
  };
}

export default function MyTicketsPage() {
  const navigate = useNavigate();
  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterGroup, setFilterGroup] = useState<'all' | 'active' | 'approval' | 'ready' | 'done'>(
    'all'
  );

  useEffect(() => {
    ticketService
      .list({ pageSize: 50 })
      .then((res) => setTickets(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, []);

  const stats = useMemo(() => {
    const waitingApproval = tickets.filter((t) => t.status === 'WAITING_APPROVAL').length;
    const readyPickup = tickets.filter((t) => t.status === 'READY_FOR_PICKUP').length;
    const active = tickets.filter(
      (t) => !['DELIVERED', 'CLOSED_REJECTED'].includes(t.status)
    ).length;
    const done = tickets.filter((t) => t.status === 'DELIVERED').length;
    return { total: tickets.length, active, waitingApproval, readyPickup, done };
  }, [tickets]);

  const filteredTickets = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return tickets.filter((t) => {
      if (filterGroup === 'active' && ['DELIVERED', 'CLOSED_REJECTED'].includes(t.status))
        return false;
      if (filterGroup === 'approval' && t.status !== 'WAITING_APPROVAL') return false;
      if (filterGroup === 'ready' && t.status !== 'READY_FOR_PICKUP') return false;
      if (filterGroup === 'done' && t.status !== 'DELIVERED') return false;

      if (q) {
        const matchCode = t.ticketCode.toLowerCase().includes(q);
        const matchDevice = t.deviceLabel.toLowerCase().includes(q);
        const matchIssue = (t.issueReported ?? '').toLowerCase().includes(q);
        return matchCode || matchDevice || matchIssue;
      }
      return true;
    });
  }, [tickets, searchQuery, filterGroup]);

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} />;

  return (
    <div className={styles.container}>
      <div className={styles.headerRow}>
        <div>
          <span className={styles.eyebrow}>THEO DÕI SỬA CHỮA — FIXORA</span>
          <h1 className={styles.title}>Phiếu sửa chữa của tôi</h1>
          <p className={styles.subtitle}>
            Theo dõi trực tuyến quy trình kiểm tra kỹ thuật, xác nhận báo giá và tiến độ hoàn thiện thiết bị của bạn.
          </p>
        </div>
        <Button onClick={() => navigate('/book')}>+ Đặt lịch sửa thiết bị</Button>
      </div>

      {tickets.length === 0 ? (
        <EmptyState message="Bạn chưa có phiếu sửa chữa nào tại Fixora." />
      ) : (
        <>
          <div className={styles.kpiGrid}>
            <button
              type="button"
              className={`${styles.kpiCard} ${filterGroup === 'all' ? styles.kpiCardActive : ''}`}
              onClick={() => setFilterGroup('all')}
            >
              <strong>{stats.total}</strong>
              <span>Tổng số phiếu</span>
            </button>
            <button
              type="button"
              className={`${styles.kpiCard} ${
                filterGroup === 'active' ? styles.kpiCardActive : ''
              }`}
              onClick={() => setFilterGroup('active')}
            >
              <strong>{stats.active}</strong>
              <span>Đang trong quy trình</span>
            </button>
            <button
              type="button"
              className={`${styles.kpiCard} ${
                filterGroup === 'approval' ? styles.kpiCardActive : ''
              } ${stats.waitingApproval > 0 ? styles.kpiCardWarning : ''}`}
              onClick={() => setFilterGroup('approval')}
            >
              <strong>{stats.waitingApproval}</strong>
              <span>Chờ bạn duyệt báo giá</span>
            </button>
            <button
              type="button"
              className={`${styles.kpiCard} ${
                filterGroup === 'ready' ? styles.kpiCardActive : ''
              }`}
              onClick={() => setFilterGroup('ready')}
            >
              <strong>{stats.readyPickup}</strong>
              <span>Sẵn sàng bàn giao</span>
            </button>
          </div>

          <div className={styles.filterBar}>
            <div className={styles.searchBox}>
              <span>🔎</span>
              <input
                type="text"
                placeholder="Tìm theo mã phiếu (VD: TK-...), tên thiết bị hoặc mô tả lỗi..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
            <div className={styles.filterPills}>
              {[
                { key: 'all', label: 'Tất cả' },
                { key: 'active', label: 'Đang sửa' },
                { key: 'approval', label: 'Chờ duyệt giá' },
                { key: 'ready', label: 'Chờ nhận máy' },
                { key: 'done', label: 'Đã hoàn tất' },
              ].map((tab) => (
                <button
                  key={tab.key}
                  type="button"
                  className={`${styles.filterPill} ${
                    filterGroup === tab.key ? styles.filterPillActive : ''
                  }`}
                  onClick={() =>
                    setFilterGroup(tab.key as 'all' | 'active' | 'approval' | 'ready' | 'done')
                  }
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </div>

          {filteredTickets.length === 0 ? (
            <EmptyState message="Không có phiếu sửa chữa nào khớp với bộ lọc hiện tại." />
          ) : (
            <div className={styles.list}>
              {filteredTickets.map((t) => {
                const progress = getTicketProgress(t.status);
                return (
                  <div
                    key={t.id}
                    className={styles.card}
                    onClick={() => navigate(`/tickets/${t.id}`)}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') navigate(`/tickets/${t.id}`);
                    }}
                  >
                    <div className={styles.cardMain}>
                      <div className={styles.deviceIcon}>{STATUS_ICON[t.status] ?? '📱'}</div>
                      <div className={styles.cardContentWrap}>
                        <div className={styles.ticketCodeRow}>
                          <strong className={styles.ticketCode}>{t.ticketCode}</strong>
                          <span className={styles.ticketDate}>
                            Tiếp nhận: {new Date(t.receivedAt).toLocaleDateString('vi-VN')}
                          </span>
                          {t.technicianName && (
                            <span className={styles.techTag}>👨‍🔧 KTV: {t.technicianName}</span>
                          )}
                        </div>
                        <p className={styles.device}>{t.deviceLabel}</p>
                        {t.issueReported && (
                          <p className={styles.issuePreview}>Tình trạng: {t.issueReported}</p>
                        )}

                        {t.status === 'WAITING_APPROVAL' && (
                          <div className={styles.attentionBanner} data-tone="warning">
                            ⚠️ Kỹ thuật viên đã gửi báo giá chi tiết. Vui lòng nhấn vào phiếu để xem và xác nhận!
                          </div>
                        )}
                        {t.status === 'READY_FOR_PICKUP' && (
                          <div className={styles.attentionBanner} data-tone="success">
                            🎉 Thiết bị của bạn đã hoàn tất sửa chữa &amp; kiểm thử QA. Mời bạn ghé cửa hàng nhận máy!
                          </div>
                        )}

                        <div className={styles.progressSection}>
                          <div className={styles.progressHeader}>
                            <span>{progress.stepText}</span>
                            <strong>{progress.percent}%</strong>
                          </div>
                          <div className={styles.progressTrack}>
                            <div
                              className={styles.progressFill}
                              style={{ width: `${progress.percent}%` }}
                            />
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className={styles.cardMeta}>
                      <Badge variant={TICKET_STATUS_BADGE_VARIANT[t.status]}>
                        {TICKET_STATUS_LABELS[t.status]}
                      </Badge>
                      <span className={styles.actionCue}>
                        {t.status === 'WAITING_APPROVAL' ? 'Duyệt báo giá →' : 'Xem chi tiết →'}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}
    </div>
  );
}
