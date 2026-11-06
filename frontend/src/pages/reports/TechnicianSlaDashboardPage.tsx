import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Badge, Button, EmptyState, ErrorMessage, Loading } from '../../components/common';
import { SLAStatusBadge, SLACountdown, SLAProgressBar } from '../../components/sla';
import { useTechnicianSlaDashboard } from '../../hooks/useSLADashboard';
import type { SlaTicketItem } from '../../types/reports.types';
import styles from './SlaRoleDashboard.module.css';

function matchSlaFilter(item: SlaTicketItem, filter: string): boolean {
  if (!filter) return true;
  if (filter === 'Overdue') return item.slaStatus === 'Overdue' || item.slaStatus === 2;
  if (filter === 'DueSoon') return item.slaStatus === 'DueSoon' || item.slaStatus === 1;
  if (filter === 'OnTrack') return item.slaStatus === 'OnTrack' || item.slaStatus === 0;
  return true;
}

export default function TechnicianSlaDashboardPage() {
  const navigate = useNavigate();
  const dashboard = useTechnicianSlaDashboard();
  const [slaFilter, setSlaFilter] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  const allItems = dashboard.data?.items ?? [];
  const recentCompleted = dashboard.data?.recentCompleted ?? [];

  const urgent = useMemo(
    () =>
      allItems.filter(
        (item) => item.slaStatus !== 'Completed' && item.slaStatus !== 3
      ),
    [allItems]
  );

  const waitingParts = useMemo(
    () => urgent.filter((item) => item.statusCode === 'WAITING_PARTS'),
    [urgent]
  );

  const filteredUrgent = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return urgent.filter((item) => {
      if (!matchSlaFilter(item, slaFilter)) return false;
      if (!q) return true;
      return (
        (item.ticketCode ?? '').toLowerCase().includes(q) ||
        (item.deviceLabel ?? '').toLowerCase().includes(q) ||
        (item.customerName ?? '').toLowerCase().includes(q) ||
        (item.statusLabel ?? '').toLowerCase().includes(q)
      );
    });
  }, [urgent, slaFilter, searchQuery]);

  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data) {
    return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  }

  const data = dashboard.data;
  if (!data) return <EmptyState message="Bạn chưa có phiếu nào cần theo dõi SLA." />;

  const overdueCount = data.overdue ?? 0;
  const dueSoonCount = data.dueSoon ?? 0;
  const onTrackCount = data.onTrack ?? 0;
  const totalActive = overdueCount + dueSoonCount + onTrackCount;
  const onTimePercent =
    totalActive > 0 ? Math.round(((onTrackCount + dueSoonCount) / totalActive) * 100) : 100;

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>SLA KỸ THUẬT VIÊN — FIXORA</span>
          <h1>Theo dõi SLA &amp; Tiến độ của tôi</h1>
          <p>
            Giám sát thời gian cam kết xử lý trên từng thiết bị được phân công cho bạn, ưu tiên các phiếu sắp chạm mốc SLA.
          </p>
        </div>
        <div className={styles.techHeaderActions}>
          {dashboard.refreshing && (
            <span className={styles.staffRefreshing}>Đang đồng bộ SLA...</span>
          )}
          <span className={styles.compliancePill}>
            Tỷ lệ trong hạn: <strong>{onTimePercent}%</strong>
          </span>
          <Button variant="secondary" onClick={() => navigate('/technician/tickets')}>
            🔧 Bàn làm việc KTV
          </Button>
        </div>
      </header>

      {/* 4 Thẻ KPI trực quan */}
      <section className={styles.kpis} aria-label="Tổng quan SLA cá nhân">
        <article
          className={`${styles.kpiCard} ${styles.kpiDanger} ${styles.clickableKpi}`}
          onClick={() => setSlaFilter(slaFilter === 'Overdue' ? '' : 'Overdue')}
        >
          <span className={styles.kpiIcon}>🚨</span>
          <div className={styles.kpiContent}>
            <strong>{overdueCount}</strong>
            <span>Phiếu quá hạn SLA</span>
          </div>
        </article>

        <article
          className={`${styles.kpiCard} ${styles.kpiWarning} ${styles.clickableKpi}`}
          onClick={() => setSlaFilter(slaFilter === 'DueSoon' ? '' : 'DueSoon')}
        >
          <span className={styles.kpiIcon}>⏱</span>
          <div className={styles.kpiContent}>
            <strong>{dueSoonCount}</strong>
            <span>Phiếu sắp trễ hạn</span>
          </div>
        </article>

        <article
          className={`${styles.kpiCard} ${styles.kpiSuccess} ${styles.clickableKpi}`}
          onClick={() => setSlaFilter(slaFilter === 'OnTrack' ? '' : 'OnTrack')}
        >
          <span className={styles.kpiIcon}>✓</span>
          <div className={styles.kpiContent}>
            <strong>{onTrackCount}</strong>
            <span>Phiếu đúng tiến độ</span>
          </div>
        </article>

        <article className={styles.kpiCard}>
          <span className={styles.kpiIcon}>📦</span>
          <div className={styles.kpiContent}>
            <strong>{waitingParts.length}</strong>
            <span>Đang chờ linh kiện</span>
          </div>
        </article>
      </section>

      {/* Khu vực Việc cần làm */}
      <section className={styles.card}>
        <div className={styles.techToolbarHeader}>
          <div className={styles.sectionHeading}>
            <h2>Danh sách phiếu đang tính giờ SLA ({filteredUrgent.length})</h2>
            <p>Nhấn vào từng thẻ hoặc nút thao tác để cập nhật kết quả chẩn đoán / sửa chữa.</p>
          </div>

          <div className={styles.techFilterRow}>
            <div className={styles.techSearchBox}>
              <span>🔎</span>
              <input
                type="text"
                placeholder="Tìm mã phiếu, thiết bị, khách hàng..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
            <div className={styles.techFilterPills}>
              {[
                { key: '', label: 'Tất cả' },
                { key: 'Overdue', label: `Quá hạn (${overdueCount})` },
                { key: 'DueSoon', label: `Sắp trễ (${dueSoonCount})` },
                { key: 'OnTrack', label: `Đúng hạn (${onTrackCount})` },
              ].map((tab) => (
                <button
                  key={tab.key}
                  type="button"
                  className={`${styles.techFilterPill} ${
                    slaFilter === tab.key ? styles.techFilterPillActive : ''
                  }`}
                  onClick={() => setSlaFilter(tab.key)}
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        {filteredUrgent.length ? (
          <div className={styles.techList}>
            {filteredUrgent.map((item) => (
              <article
                className={`${styles.techCard} ${
                  item.slaStatus === 'Overdue' || item.slaStatus === 2
                    ? styles.techCardOverdue
                    : item.slaStatus === 'DueSoon' || item.slaStatus === 1
                      ? styles.techCardDueSoon
                      : ''
                }`}
                key={item.ticketId}
              >
                <div className={styles.techCardHeader}>
                  <div>
                    <Link to={`/tickets/${item.ticketId}`} className={styles.techCodeLink}>
                      {item.ticketCode}
                    </Link>
                    <span className={styles.techCustomerName}> · {item.customerName}</span>
                  </div>
                  <SLAStatusBadge status={item.slaStatus} />
                </div>

                <div className={styles.techDeviceRow}>
                  <span className={styles.techDevicePill}>💻 {item.deviceLabel}</span>
                  <span className={styles.workflowStepPill}>{item.statusLabel}</span>
                </div>

                <div className={styles.techCountdownBox}>
                  <div>
                    <span className={styles.techTimerLabel}>Thời gian SLA còn lại</span>
                    <strong className={styles.largeCountdown}>
                      <SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} />
                    </strong>
                  </div>
                  <span
                    className={`${styles.clockStateTag} ${
                      item.clockRunning ? styles.clockRunning : styles.clockPaused
                    }`}
                  >
                    {item.clockRunning ? '● Đang tính giờ' : '⏸ Tạm dừng'}
                  </span>
                </div>

                <SLAProgressBar
                  startedAt={item.startedAt}
                  dueAt={item.dueAt}
                  slaStatus={item.slaStatus}
                />

                <div className={styles.techCardFooter}>
                  <span className={styles.techDueTime}>
                    Hạn mục tiêu: {new Date(item.dueAt).toLocaleString('vi-VN')}
                  </span>
                  <Button
                    size="sm"
                    onClick={() => navigate(`/tickets/${item.ticketId}`)}
                  >
                    Cập nhật phiếu →
                  </Button>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <EmptyState message="Không có phiếu nào khớp với bộ lọc SLA hiện tại." />
        )}
      </section>

      {/* 2 cột bên dưới: Chờ linh kiện & Đã hoàn tất gần đây */}
      <section className={styles.breakdownGrid} style={{ marginTop: 20 }}>
        <article className={styles.card} style={{ marginTop: 0 }}>
          <div className={styles.sectionHeading}>
            <h2>📦 Đang chờ linh kiện ({waitingParts.length})</h2>
            <p>Các phiếu đang tạm dừng hoặc theo dõi riêng trong thời gian chờ kho cấp phát linh kiện.</p>
          </div>
          {waitingParts.length ? (
            <div className={styles.subCardList}>
              {waitingParts.map((item) => (
                <div key={item.ticketId} className={styles.subTicketRow}>
                  <div className={styles.subTicketInfo}>
                    <div className={styles.subTicketTop}>
                      <Link to={`/tickets/${item.ticketId}`} className={styles.techCodeLink}>
                        {item.ticketCode}
                      </Link>
                      <Badge variant={item.clockRunning ? 'warning' : 'default'}>
                        {item.clockRunning ? 'Đồng hồ đang chạy' : 'Đồng hồ tạm dừng'}
                      </Badge>
                    </div>
                    <span className={styles.subTicketMeta}>
                      {item.deviceLabel} · Khách: {item.customerName}
                    </span>
                  </div>
                  <div className={styles.subTicketRight}>
                    <strong className={styles.subCountdown}>
                      <SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} />
                    </strong>
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => navigate(`/tickets/${item.ticketId}`)}
                    >
                      Mở phiếu
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState message="Hiện không có phiếu nào đang chờ linh kiện." />
          )}
        </article>

        <article className={styles.card} style={{ marginTop: 0 }}>
          <div className={styles.sectionHeading}>
            <h2>✅ Đã hoàn tất gần đây ({recentCompleted.length})</h2>
            <p>Lịch sử tuân thủ SLA của các phiếu bạn vừa hoàn thành bàn giao kỹ thuật.</p>
          </div>
          {recentCompleted.length ? (
            <div className={styles.subCardList}>
              {recentCompleted.map((item) => (
                <div key={item.ticket.ticketId} className={styles.subTicketRow}>
                  <div className={styles.subTicketInfo}>
                    <div className={styles.subTicketTop}>
                      <Link
                        to={`/tickets/${item.ticket.ticketId}`}
                        className={styles.techCodeLink}
                      >
                        {item.ticket.ticketCode}
                      </Link>
                      <Badge variant={item.completedOnTime ? 'success' : 'danger'}>
                        {item.completedOnTime ? '✓ Đúng hạn SLA' : '⚠ Hoàn tất trễ'}
                      </Badge>
                    </div>
                    <span className={styles.subTicketMeta}>
                      {item.ticket.deviceLabel} · {item.ticket.customerName}
                    </span>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => navigate(`/tickets/${item.ticket.ticketId}`)}
                  >
                    Xem lại →
                  </Button>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState message="Chưa có dữ liệu phiếu hoàn tất gần đây." />
          )}
        </article>
      </section>
    </main>
  );
}
