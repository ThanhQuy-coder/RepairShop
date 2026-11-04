import { Link } from 'react-router-dom';
import { Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { SLAFilterBar, SLAStatusBadge, SLACountdown, SLAProgressBar } from '../../components/sla';
import { useStaffSlaDashboard } from '../../hooks/useSLADashboard';
import styles from './SlaRoleDashboard.module.css';

export default function ReceptionistSlaDashboardPage() {
  const dashboard = useStaffSlaDashboard();
  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data) return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  const items = dashboard.data?.items ?? [];
  const overdue = items.filter((item) => item.slaStatus === 'Overdue' || item.slaStatus === 2).length;
  const dueSoon = items.filter((item) => item.slaStatus === 'DueSoon' || item.slaStatus === 1).length;
  const groups = [{ title: 'Chờ khách phản hồi', code: 'WAITING_APPROVAL' }, { title: 'Sẵn sàng bàn giao', code: 'READY_FOR_PICKUP' }, { title: 'Đang chờ phân công / tiếp nhận', code: 'CHECKED_IN' }];
  return <main className={styles.page}><header className={styles.header}><div><span className={styles.eyebrow}>SLA LỄ TÂN</span><h1>Điều phối phiếu cần xử lý</h1><p>Chỉ hiển thị các phiếu trong phạm vi tài khoản của bạn.</p></div></header>
    {dashboard.refreshing && <small className={styles.refreshing}>Đang cập nhật dữ liệu nền...</small>}
    {(overdue > 0 || dueSoon > 0) && <div className={styles.alerts}>{overdue > 0 && <strong>🚨 Có {overdue} phiếu quá hạn cần xử lý</strong>}{dueSoon > 0 && <span>⏱ {dueSoon} phiếu sắp trễ</span>}</div>}
    <section className={styles.kanban}>{groups.map((group) => <article className={styles.kanbanColumn} key={group.code}><h2>{group.title}</h2>{items.filter((item) => item.statusCode === group.code).sort((a, b) => Number(b.slaStatus === 'Overdue') - Number(a.slaStatus === 'Overdue')).map((item) => <div className={styles.ticketCard} key={item.ticketId}><Link to={`/tickets/${item.ticketId}`}><strong>{item.ticketCode}</strong></Link><span>{item.customerName} · {item.deviceLabel}</span><SLAStatusBadge status={item.slaStatus} /><strong><SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} /></strong><SLAProgressBar startedAt={item.startedAt} dueAt={item.dueAt} slaStatus={item.slaStatus} /><Button size="sm" variant="secondary" onClick={() => navigator.clipboard?.writeText(item.customerName)}>Sao chép SĐT khách</Button></div>)}</article>)}</section>
    <section className={styles.card}><h2>Tất cả phiếu theo dõi SLA</h2><SLAFilterBar filters={dashboard.filters} onChange={dashboard.setFilters} onClear={() => dashboard.setFilters({ slaStatus: '', statusCode: '', deviceType: '' })} />{items.length ? <div className={styles.tableScroll}><table><thead><tr><th>Phiếu</th><th>Khách hàng / thiết bị</th><th>Bước</th><th>SLA</th><th>Phụ trách</th></tr></thead><tbody>{items.map((item) => <tr key={item.ticketId}><td><Link to={`/tickets/${item.ticketId}`}>{item.ticketCode}</Link></td><td>{item.customerName}<br />{item.deviceLabel}</td><td>{item.statusLabel}</td><td><SLAStatusBadge status={item.slaStatus} /> <SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} /></td><td>{item.technicianName ?? 'Chưa phân công'}</td></tr>)}</tbody></table></div> : <EmptyState message="Bạn chưa có phiếu nào cần theo dõi SLA." />}<Pagination page={dashboard.page} pageSize={10} total={dashboard.data?.total ?? 0} onPageChange={dashboard.setPage} /></section>
  </main>;
}
