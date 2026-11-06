import { Link } from 'react-router-dom';
import {
  Button,
  EmptyState,
  ErrorMessage,
  Loading,
  Pagination,
  Table,
} from '../../components/common';
import { SLAFilterBar, SLATicketRow } from '../../components/sla';
import { useAdminSlaDashboard } from '../../hooks/useSLADashboard';
import styles from './SlaRoleDashboard.module.css';

const deviceLabels: Record<string, string> = {
  Phone: 'Điện thoại',
  Laptop: 'Laptop',
  Tablet: 'Máy tính bảng',
  Electronics: 'Điện tử khác',
  Smartwatch: 'Đồng hồ thông minh',
};

export default function AdminSlaDashboardPage() {
  const dashboard = useAdminSlaDashboard();
  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data)
    return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  const data = dashboard.data;
  if (!data) return <EmptyState message="Chưa có dữ liệu SLA." />;
  const total = data.onTrack + data.dueSoon + data.overdue;
  const hasSlaData = total > 0;
  const complianceRate = Math.max(0, Math.min(100, data.complianceRate));

  const filterByStatus = (slaStatus: string) => {
    dashboard.setFilters({ ...dashboard.filters, slaStatus });
  };

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>SLA TOÀN HỆ THỐNG</span>
          <h1>Dashboard SLA &amp; Quá hạn</h1>
          <p>Theo dõi hiệu suất xử lý, nút thắt và các phiếu cần ưu tiên.</p>
        </div>
        <Link to="/admin/sla-policies">
          <Button>Cấu hình SLA</Button>
        </Link>
      </header>

      {dashboard.refreshing && (
        <small className={styles.refreshing}>Đang cập nhật dữ liệu nền...</small>
      )}

      <section className={styles.kpis} aria-label="Tổng quan SLA">
        <KpiCard icon="▣" label="Tổng ticket đang theo dõi" value={data.total} tone="neutral" />
        <KpiCard icon="✓" label="Đúng hạn" value={data.onTrack} tone="success" />
        <KpiCard icon="◷" label="Sắp trễ" value={data.dueSoon} tone="warning" />
        <KpiCard icon="!" label="Quá hạn" value={data.overdue} tone="danger" />
      </section>

      <section className={styles.analyticsGrid}>
        <article className={`${styles.card} ${styles.distributionCard}`}>
          <SectionHeading
            title="Phân bố SLA"
            description="Tình trạng các ticket đang được theo dõi"
          />
          {hasSlaData ? (
            <div className={styles.donutLayout}>
              <div className={styles.donutWrap}>
                <svg
                  viewBox="0 0 42 42"
                  className={styles.donut}
                  role="img"
                  aria-label={`${data.overdue} phiếu quá hạn trên ${total}`}
                >
                  <circle className={styles.donutBase} cx="21" cy="21" r="15.9" />
                  <circle
                    className={styles.donutDanger}
                    cx="21"
                    cy="21"
                    r="15.9"
                    strokeDasharray={`${(data.overdue / total) * 100} ${100 - (data.overdue / total) * 100}`}
                  />
                  <circle
                    className={styles.donutWarning}
                    cx="21"
                    cy="21"
                    r="15.9"
                    strokeDasharray={`${(data.dueSoon / total) * 100} ${100 - (data.dueSoon / total) * 100}`}
                    strokeDashoffset={`${(-data.overdue / total) * 100}`}
                  />
                  <circle
                    className={styles.donutSuccess}
                    cx="21"
                    cy="21"
                    r="15.9"
                    strokeDasharray={`${(data.onTrack / total) * 100} ${100 - (data.onTrack / total) * 100}`}
                    strokeDashoffset={`${(-(data.overdue + data.dueSoon) / total) * 100}`}
                  />
                </svg>
                <div className={styles.donutCenter}>
                  <strong>{complianceRate}%</strong>
                  <span>tuân thủ</span>
                </div>
              </div>
              <div className={styles.legend}>
                <LegendItem
                  tone="success"
                  label="Đúng hạn"
                  value={data.onTrack}
                  onClick={() => filterByStatus('OnTrack')}
                />
                <LegendItem
                  tone="warning"
                  label="Sắp trễ"
                  value={data.dueSoon}
                  onClick={() => filterByStatus('DueSoon')}
                />
                <LegendItem
                  tone="danger"
                  label="Quá hạn"
                  value={data.overdue}
                  onClick={() => filterByStatus('Overdue')}
                />
              </div>
            </div>
          ) : (
            <EmptyState message="Chưa có ticket SLA đang được theo dõi." />
          )}
        </article>

        <article className={styles.card}>
          <SectionHeading
            title="Nút thắt theo bước workflow"
            description="Tỷ lệ ticket quá hạn ở từng bước xử lý"
          />
          {data.byStatusStep.length ? (
            <div className={styles.breakdownList}>
              {data.byStatusStep.map((step) => (
                <BreakdownRow
                  key={step.statusCode}
                  label={step.label}
                  total={step.total}
                  overdue={step.overdue}
                />
              ))}
            </div>
          ) : (
            <EmptyState message="Chưa có bước SLA đang theo dõi." />
          )}
        </article>
      </section>

      <section className={styles.breakdownGrid}>
        <article className={styles.card}>
          <SectionHeading
            title="Theo loại thiết bị"
            description="So sánh tổng ticket và số ticket quá hạn"
          />
          {data.byDeviceType.length ? (
            <div className={styles.breakdownList}>
              {data.byDeviceType.map((item) => (
                <BreakdownRow
                  key={item.deviceType}
                  label={deviceLabels[item.deviceType] ?? 'Thiết bị khác'}
                  total={item.total}
                  overdue={item.overdue}
                />
              ))}
            </div>
          ) : (
            <EmptyState message="Chưa có dữ liệu theo loại thiết bị." />
          )}
        </article>

        <article className={styles.card}>
          <SectionHeading
            title="Theo kỹ thuật viên"
            description="Hiệu suất xử lý theo người phụ trách"
          />
          {data.byTechnician.length ? (
            <Table
              data={data.byTechnician}
              keyExtractor={(row) => row.technicianName}
              columns={[
                { key: 'name', header: 'Kỹ thuật viên', render: (row) => row.technicianName },
                { key: 'active', header: 'Đang xử lý', render: (row) => row.inProgress },
                { key: 'due', header: 'Sắp trễ', render: (row) => row.dueSoon },
                { key: 'overdue', header: 'Quá hạn', render: (row) => row.overdue },
                { key: 'rate', header: 'Đúng hạn', render: (row) => `${row.onTimeRate}%` },
              ]}
            />
          ) : (
            <div className={styles.emptyState}>
              <span className={styles.emptyIcon}>⌁</span>
              <strong>Chưa có dữ liệu kỹ thuật viên</strong>
              <p>Dữ liệu sẽ xuất hiện khi ticket được phân công xử lý.</p>
            </div>
          )}
        </article>
      </section>

      <section className={`${styles.card} ${styles.actionCard}`}>
        <div className={styles.actionHeading}>
          <SectionHeading
            title="Ticket cần xử lý ngay"
            description="Ưu tiên các ticket sắp trễ và quá hạn"
          />
          {dashboard.tickets && (
            <span className={styles.resultCount}>{dashboard.tickets.total} ticket</span>
          )}
        </div>
        <SLAFilterBar
          filters={dashboard.filters}
          onChange={dashboard.setFilters}
          onClear={() => dashboard.setFilters({ slaStatus: '', statusCode: '', deviceType: '' })}
        />
        {dashboard.tickets?.items.length ? (
          <div className={styles.tableScroll}>
            <table className={styles.ticketTable}>
              <thead>
                <tr>
                  <th>Ticket</th>
                  <th>Khách hàng / thiết bị</th>
                  <th>Workflow</th>
                  <th>SLA &amp; thời gian</th>
                  <th>Kỹ thuật viên</th>
                  <th aria-label="Thao tác" />
                </tr>
              </thead>
              <tbody>
                {dashboard.tickets.items.map((ticket) => (
                  <SLATicketRow key={ticket.ticketId} ticket={ticket} />
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState message="Không có ticket phù hợp với bộ lọc hiện tại." />
        )}
        <Pagination
          page={dashboard.page}
          pageSize={10}
          total={dashboard.tickets?.total ?? 0}
          onPageChange={dashboard.setPage}
        />
      </section>
    </main>
  );
}

function KpiCard({
  icon,
  label,
  value,
  tone,
}: {
  icon: string;
  label: string;
  value: number;
  tone: 'neutral' | 'success' | 'warning' | 'danger';
}) {
  return (
    <article
      className={`${styles.kpiCard} ${styles[`kpi${tone[0].toUpperCase()}${tone.slice(1)}`]}`}
    >
      <span className={styles.kpiIcon}>{icon}</span>
      <div className={styles.kpiContent}>
        <strong>{value}</strong>
        <span>{label}</span>
      </div>
    </article>
  );
}

function SectionHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className={styles.sectionHeading}>
      <h2>{title}</h2>
      <p>{description}</p>
    </div>
  );
}

function LegendItem({
  tone,
  label,
  value,
  onClick,
}: {
  tone: 'success' | 'warning' | 'danger';
  label: string;
  value: number;
  onClick: () => void;
}) {
  return (
    <button className={styles.legendItem} onClick={onClick}>
      <span
        className={`${styles.legendDot} ${styles[`dot${tone[0].toUpperCase()}${tone.slice(1)}`]}`}
      />
      <span>{label}</span>
      <strong>{value}</strong>
    </button>
  );
}

function BreakdownRow({
  label,
  total,
  overdue,
}: {
  label: string;
  total: number;
  overdue: number;
}) {
  const percentage = total ? Math.round((overdue / total) * 100) : 0;
  return (
    <div className={styles.breakdownRow}>
      <div className={styles.breakdownLabel}>
        <span>{label}</span>
        <strong>{total} ticket</strong>
      </div>
      <div
        className={styles.progressTrack}
        role="progressbar"
        aria-label={`${label}: ${percentage}% quá hạn`}
        aria-valuenow={percentage}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <span style={{ width: `${percentage}%` }} />
      </div>
      <div className={styles.breakdownMeta}>
        <span>{overdue} quá hạn</span>
        <strong>{percentage}%</strong>
      </div>
    </div>
  );
}
