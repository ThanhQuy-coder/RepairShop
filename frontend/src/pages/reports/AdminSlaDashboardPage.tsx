import { Link } from 'react-router-dom';
import { Button, EmptyState, ErrorMessage, Loading, Pagination, Table } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import { SLAFilterBar, SLATicketRow } from '../../components/sla';
import { useAdminSlaDashboard } from '../../hooks/useSLADashboard';
import styles from './SlaRoleDashboard.module.css';

const deviceLabels: Record<string, string> = {
  Phone: 'Điện thoại', Laptop: 'Laptop', Tablet: 'Máy tính bảng',
  Electronics: 'Điện tử khác', Smartwatch: 'Đồng hồ thông minh',
};

export default function AdminSlaDashboardPage() {
  const dashboard = useAdminSlaDashboard();
  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data) return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  const data = dashboard.data;
  if (!data) return <EmptyState message="Chưa có dữ liệu SLA." />;
  const total = Math.max(1, data.onTrack + data.dueSoon + data.overdue);
  return <main className={styles.page}>
    <header className={styles.header}><div><span className={styles.eyebrow}>SLA TOÀN HỆ THỐNG</span><h1>Dashboard SLA & Quá hạn</h1><p>Theo dõi nút thắt và các phiếu cần xử lý ngay.</p></div><Link to="/admin/sla-policies"><Button>Cấu hình SLA</Button></Link></header>
    {dashboard.refreshing && <small className={styles.refreshing}>Đang cập nhật dữ liệu nền...</small>}
    <section className={styles.kpis}>
      <SummaryCard icon="📋" label="Tổng ticket đang theo dõi" value={data.total} />
      <SummaryCard icon="✅" label="Đúng hạn" value={data.onTrack} />
      <SummaryCard icon="⏱" label="Sắp trễ" value={data.dueSoon} />
      <SummaryCard icon="🚨" label="Quá hạn" value={data.overdue} isEmphasized />
    </section>
    <section className={styles.gridTwo}>
      <article className={styles.card}><h2>Phân bố SLA</h2><div className={styles.donutArea}><svg viewBox="0 0 42 42" className={styles.donut} role="img" aria-label={`${data.overdue} phiếu quá hạn trên ${data.total}`}><circle className={styles.donutBase} cx="21" cy="21" r="15.9" /><circle className={styles.donutDanger} cx="21" cy="21" r="15.9" strokeDasharray={`${data.overdue / total * 100} ${100 - data.overdue / total * 100}`} /><circle className={styles.donutWarning} cx="21" cy="21" r="15.9" strokeDasharray={`${data.dueSoon / total * 100} ${100 - data.dueSoon / total * 100}`} strokeDashoffset={`${-data.overdue / total * 100}`} /><circle className={styles.donutSuccess} cx="21" cy="21" r="15.9" strokeDasharray={`${data.onTrack / total * 100} ${100 - data.onTrack / total * 100}`} strokeDashoffset={`${-(data.overdue + data.dueSoon) / total * 100}`} /></svg><ul><li><button onClick={() => dashboard.setFilters({ ...dashboard.filters, slaStatus: 'OnTrack' })}>✅ Đúng hạn: {data.onTrack}</button></li><li><button onClick={() => dashboard.setFilters({ ...dashboard.filters, slaStatus: 'DueSoon' })}>⏱ Sắp trễ: {data.dueSoon}</button></li><li><button onClick={() => dashboard.setFilters({ ...dashboard.filters, slaStatus: 'Overdue' })}>🚨 Quá hạn: {data.overdue}</button></li><li><strong>Tỷ lệ tuân thủ: {data.complianceRate}%</strong></li></ul></div></article>
      <article className={styles.card}><h2>Nút thắt theo bước workflow</h2>{data.byStatusStep.length === 0 ? <EmptyState message="Chưa có bước SLA đang theo dõi." /> : data.byStatusStep.map((step) => <div className={styles.barRow} key={step.statusCode}><div><span>{step.label}</span><strong>{step.overdue}/{step.total} quá hạn</strong></div><div className={styles.bar}><span style={{ width: `${step.total ? step.overdue / step.total * 100 : 0}%` }} /></div></div>)}</article>
    </section>
    <section className={styles.card}><h2>Theo loại thiết bị</h2><div className={styles.deviceBars}>{data.byDeviceType.map((item) => <div key={item.deviceType} className={styles.barRow}><div><span>{deviceLabels[item.deviceType] ?? 'Thiết bị khác'}</span><strong>{item.overdue}/{item.total} quá hạn</strong></div><div className={styles.bar}><span style={{ width: `${item.total ? item.overdue / item.total * 100 : 0}%` }} /></div></div>)}</div></section>
    <section className={styles.card}><h2>Theo kỹ thuật viên</h2><Table data={data.byTechnician} keyExtractor={(row) => row.technicianName} columns={[{ key: 'name', header: 'Tên', render: (row) => row.technicianName }, { key: 'active', header: 'Đang xử lý', render: (row) => row.inProgress }, { key: 'due', header: 'Sắp trễ', render: (row) => row.dueSoon }, { key: 'overdue', header: 'Quá hạn', render: (row) => row.overdue }, { key: 'rate', header: '% đúng hạn', render: (row) => `${row.onTimeRate}%` }]} emptyMessage="Chưa có dữ liệu kỹ thuật viên." /></section>
    <section className={styles.card}><h2>Ticket cần xử lý ngay</h2><SLAFilterBar filters={dashboard.filters} onChange={dashboard.setFilters} onClear={() => dashboard.setFilters({ slaStatus: '', statusCode: '', deviceType: '' })} />{dashboard.tickets?.items.length ? <div className={styles.tableScroll}><table><tbody>{dashboard.tickets.items.map((ticket) => <SLATicketRow key={ticket.ticketId} ticket={ticket} />)}</tbody></table></div> : <EmptyState message="Không có ticket sắp trễ hoặc quá hạn." />}<Pagination page={dashboard.page} pageSize={10} total={dashboard.tickets?.total ?? 0} onPageChange={dashboard.setPage} /></section>
  </main>;
}
