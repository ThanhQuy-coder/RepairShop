import { Link } from 'react-router-dom';
import { Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { SLAFilterBar, SLAStatusBadge, SLACountdown, SLAProgressBar } from '../../components/sla';
import { useStaffSlaDashboard } from '../../hooks/useSLADashboard';
import type { SlaTicketItem } from '../../types/reports.types';
import styles from './SlaRoleDashboard.module.css';

const groups = [
  { title: 'Chờ khách phản hồi', code: 'WAITING_APPROVAL' },
  { title: 'Sẵn sàng bàn giao', code: 'READY_FOR_PICKUP' },
  { title: 'Đang tiếp nhận', code: 'CHECKED_IN' },
];

const emptyFilters = { slaStatus: '', statusCode: '', deviceType: '' };

export default function ReceptionistSlaDashboardPage() {
  const dashboard = useStaffSlaDashboard();

  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data) {
    return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  }

  const items = dashboard.data?.items ?? [];
  const overdue = items.filter((item) => isSlaStatus(item, 'Overdue', 2)).length;
  const dueSoon = items.filter((item) => isSlaStatus(item, 'DueSoon', 1)).length;
  const onTrack = items.filter((item) => isSlaStatus(item, 'OnTrack', 0)).length;

  return (
    <main className={styles.staffPage}>
      <header className={styles.staffHeader}>
        <div>
          <span className={styles.staffEyebrow}>SLA LỄ TÂN</span>
          <h1>Điều phối phiếu cần xử lý</h1>
          <p>Ưu tiên các phiếu sắp trễ và quá hạn trong phạm vi tài khoản của bạn.</p>
        </div>
        <div className={styles.staffHeaderMeta}>
          {dashboard.refreshing && <span className={styles.staffRefreshing}>Đang cập nhật...</span>}
          <span className={styles.scopeBadge}>Trang hiện tại · {items.length} phiếu</span>
        </div>
      </header>

      {(overdue > 0 || dueSoon > 0) && (
        <section className={styles.staffAlert} aria-label="Cảnh báo SLA">
          <span className={styles.staffAlertIcon}>!</span>
          <div>
            <strong>
              {overdue > 0 ? `${overdue} phiếu quá hạn cần xử lý` : 'Chưa có phiếu quá hạn'}
            </strong>
            {dueSoon > 0 && <span>{dueSoon} phiếu đang tiến gần hạn xử lý.</span>}
          </div>
        </section>
      )}

      <section className={styles.staffKpis} aria-label="Tổng quan SLA">
        <StaffKpi label="Tổng phiếu" value={dashboard.data?.total ?? items.length} tone="neutral" />
        <StaffKpi label="Đúng hạn" value={onTrack} tone="success" />
        <StaffKpi label="Sắp trễ" value={dueSoon} tone="warning" />
        <StaffKpi label="Quá hạn" value={overdue} tone="danger" />
      </section>

      <section className={styles.staffSection}>
        <div className={styles.staffSectionHeader}>
          <div>
            <h2>Hàng đợi theo trạng thái</h2>
            <p>Nhóm nhanh các phiếu cần lễ tân theo dõi.</p>
          </div>
          <span className={styles.sectionHint}>Chọn phiếu để xem chi tiết</span>
        </div>
        <div className={styles.staffBoard}>
          {groups.map((group) => {
            const groupItems = items
              .filter((item) => item.statusCode === group.code)
              .sort(
                (left, right) =>
                  Number(isSlaStatus(right, 'Overdue', 2)) - Number(isSlaStatus(left, 'Overdue', 2))
              );

            return (
              <article className={styles.staffColumn} key={group.code}>
                <div className={styles.staffColumnHeader}>
                  <h3>{group.title}</h3>
                  <span>{groupItems.length}</span>
                </div>
                {groupItems.length ? (
                  <div className={styles.staffTicketList}>
                    {groupItems.map((item) => (
                      <StaffTicketCard key={item.ticketId} item={item} />
                    ))}
                  </div>
                ) : (
                  <div className={styles.staffColumnEmpty}>Không có phiếu trong nhóm này.</div>
                )}
              </article>
            );
          })}
        </div>
      </section>

      <section className={styles.staffSection}>
        <div className={styles.staffSectionHeader}>
          <div>
            <h2>Tất cả phiếu theo dõi SLA</h2>
            <p>Lọc theo trạng thái, thiết bị hoặc bước workflow.</p>
          </div>
          {dashboard.error && <span className={styles.staffInlineError}>{dashboard.error}</span>}
        </div>
        <SLAFilterBar
          filters={dashboard.filters}
          onChange={dashboard.setFilters}
          onClear={() => dashboard.setFilters(emptyFilters)}
        />
        {items.length ? (
          <div className={styles.staffTableScroll}>
            <table className={styles.staffTable}>
              <thead>
                <tr>
                  <th>Phiếu</th>
                  <th>Khách hàng / thiết bị</th>
                  <th>Bước workflow</th>
                  <th>SLA &amp; thời gian</th>
                  <th>Phụ trách</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.ticketId}>
                    <td>
                      <Link to={`/tickets/${item.ticketId}`}>{item.ticketCode}</Link>
                    </td>
                    <td>
                      <strong>{item.customerName}</strong>
                      <small>{item.deviceLabel}</small>
                    </td>
                    <td>{item.statusLabel}</td>
                    <td>
                      <SLAStatusBadge status={item.slaStatus} />
                      <span className={styles.staffCountdown}>
                        <SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} />
                      </span>
                      <SLAProgressBar
                        startedAt={item.startedAt}
                        dueAt={item.dueAt}
                        slaStatus={item.slaStatus}
                      />
                    </td>
                    <td>{item.technicianName ?? 'Chưa phân công'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState message="Bạn chưa có phiếu nào phù hợp với bộ lọc hiện tại." />
        )}
        <Pagination
          page={dashboard.page}
          pageSize={10}
          total={dashboard.data?.total ?? 0}
          onPageChange={dashboard.setPage}
        />
      </section>
    </main>
  );
}

function isSlaStatus(item: SlaTicketItem, status: string, legacyValue: number) {
  return item.slaStatus === status || item.slaStatus === legacyValue;
}

function StaffKpi({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: 'neutral' | 'success' | 'warning' | 'danger';
}) {
  return (
    <article
      className={`${styles.staffKpi} ${styles[`staffKpi${tone[0].toUpperCase()}${tone.slice(1)}`]}`}
    >
      <span>{value}</span>
      <strong>{label}</strong>
    </article>
  );
}

function StaffTicketCard({ item }: { item: SlaTicketItem }) {
  return (
    <div className={styles.staffTicketCard}>
      <div className={styles.staffTicketTopline}>
        <Link to={`/tickets/${item.ticketId}`}>{item.ticketCode}</Link>
        <SLAStatusBadge status={item.slaStatus} />
      </div>
      <strong className={styles.staffCustomer}>{item.customerName}</strong>
      <span className={styles.staffDevice}>{item.deviceLabel}</span>
      <div className={styles.staffDue}>
        <span>Thời gian</span>
        <strong>
          <SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} />
        </strong>
      </div>
      <SLAProgressBar startedAt={item.startedAt} dueAt={item.dueAt} slaStatus={item.slaStatus} />
      <Button
        size="sm"
        variant="secondary"
        onClick={() => window.location.assign(`/tickets/${item.ticketId}`)}
      >
        Xem phiếu
      </Button>
    </div>
  );
}
