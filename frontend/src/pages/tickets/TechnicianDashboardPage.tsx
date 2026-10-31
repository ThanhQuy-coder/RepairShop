import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { Badge, Loading, ErrorMessage, EmptyState, Button } from '../../components/common';
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
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [isRefreshing, setIsRefreshing] = useState(false);

  const loadTickets = useCallback((showLoading = true) => {
    if (showLoading) setIsLoading(true);
    setIsRefreshing(true);
    setErrorMessage(null);
    ticketService
      .list({ pageSize: 200 })
      .then((res) => setTickets(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => {
        setIsLoading(false);
        setIsRefreshing(false);
      });
  }, []);

  useEffect(() => {
    loadTickets();
  }, [loadTickets]);

  const filteredTickets = useMemo(() => {
    const query = normalize(search);
    return tickets.filter((ticket) => {
      const searchableText = [
        ticket.ticketCode,
        ticket.customerName,
        ticket.deviceLabel,
        ticket.issueReported,
        TICKET_STATUS_LABELS[ticket.status],
      ].map(normalize).join(' ');
      const matchesStatus = statusFilter === 'ALL' || ticket.status === statusFilter;
      const matchesSearch = !query || searchableText.includes(query);
      return matchesStatus && matchesSearch;
    });
  }, [tickets, search, statusFilter]);
  const isFiltering = Boolean(search.trim()) || statusFilter !== 'ALL';
  const visibleGroups = isFiltering
    ? GROUPS.filter((statusCode) => filteredTickets.some((ticket) => ticket.status === statusCode))
    : GROUPS;

  const activeCount = tickets.filter((ticket) => !['READY_FOR_PICKUP', 'DELIVERED', 'CLOSED_REJECTED'].includes(ticket.status)).length;
  const qaCount = tickets.filter((ticket) => ticket.status === 'QA_TESTING').length;
  const waitingCount = tickets.filter((ticket) => ['ASSIGNED', 'WAITING_PARTS'].includes(ticket.status)).length;

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
        <Button variant="secondary" size="sm" onClick={() => loadTickets(false)} isLoading={isRefreshing}>
          ↻ Làm mới
        </Button>
      </div>

      <div className={styles.quickStats}>
        <div className={styles.quickStat}><strong>{activeCount}</strong><span>Đang xử lý</span></div>
        <div className={styles.quickStat}><strong>{waitingCount}</strong><span>Cần bắt đầu / chờ linh kiện</span></div>
        <div className={styles.quickStat}><strong>{qaCount}</strong><span>Đang chờ QA</span></div>
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

      <div className={styles.toolbar}>
        <label className={styles.searchBox}>
          <span aria-hidden="true">⌕</span>
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm mã ticket, khách hàng, thiết bị..."
            aria-label="Tìm ticket"
          />
        </label>
        <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} aria-label="Lọc trạng thái">
          <option value="ALL">Tất cả trạng thái</option>
          {GROUPS.map((statusCode) => (
            <option key={statusCode} value={statusCode}>{TICKET_STATUS_LABELS[statusCode]}</option>
          ))}
        </select>
        {isFiltering && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearch('');
              setStatusFilter('ALL');
            }}
          >
            Xóa lọc
          </Button>
        )}
      </div>
      {isFiltering && (
        <p className={styles.resultSummary}>
          Hiển thị <strong>{filteredTickets.length}</strong> / {tickets.length} ticket
        </p>
      )}

      <div className={styles.groupContainer}>
        {visibleGroups.map((statusCode) => {
          const items = filteredTickets.filter((t) => t.status === statusCode);
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
                      role="button"
                      tabIndex={0}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') navigate(`/tickets/${t.id}`);
                      }}
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
                        <span className={styles.receivedAt}>
                          Tiếp nhận {new Date(t.receivedAt).toLocaleDateString('vi-VN')}
                        </span>
                      </div>
                      {t.issueReported && <p className={styles.issue}>{t.issueReported}</p>}
                      <div className={styles.cardFooter}>
                        <span>{t.status === 'ASSIGNED' ? 'Bắt đầu chẩn đoán' : t.status === 'QA_TESTING' ? 'Thực hiện QA' : 'Mở hồ sơ'}</span>
                        <span aria-hidden="true">→</span>
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
      {tickets.length > 0 && filteredTickets.length === 0 && (
        <EmptyState message="Không tìm thấy ticket phù hợp với bộ lọc hiện tại." />
      )}
    </div>
  );
}

function normalize(value: string | null | undefined) {
  return (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase()
    .trim();
}
