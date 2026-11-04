import { Link } from 'react-router-dom';
import { EmptyState, ErrorMessage, Loading } from '../../components/common';
import { SLAStatusBadge, SLACountdown, SLAProgressBar } from '../../components/sla';
import { useTechnicianSlaDashboard } from '../../hooks/useSLADashboard';
import styles from './SlaRoleDashboard.module.css';

export default function TechnicianSlaDashboardPage() {
  const dashboard = useTechnicianSlaDashboard();
  if (dashboard.loading) return <Loading />;
  if (dashboard.error && !dashboard.data) return <ErrorMessage message={dashboard.error} onRetry={dashboard.retry} />;
  const data = dashboard.data;
  if (!data) return <EmptyState message="Bạn chưa có phiếu nào cần theo dõi SLA." />;
  const urgent = data.items.filter((item) => item.slaStatus !== 'Completed' && item.slaStatus !== 3);
  const waitingParts = urgent.filter((item) => item.statusCode === 'WAITING_PARTS');
  return <main className={styles.page}><header className={styles.header}><div><span className={styles.eyebrow}>SLA KỸ THUẬT VIÊN</span><h1>Việc cần làm</h1><p>Danh sách chỉ gồm phiếu được phân công cho bạn.</p></div></header>{dashboard.refreshing && <small className={styles.refreshing}>Đang cập nhật dữ liệu nền...</small>}
    <section className={styles.kpis}><div className={styles.personalCard}>🚨 <strong>{data.overdue}</strong><span>Quá hạn</span></div><div className={styles.personalCard}>⏱ <strong>{data.dueSoon}</strong><span>Sắp trễ</span></div><div className={styles.personalCard}>✅ <strong>{data.onTrack}</strong><span>Đúng hạn</span></div></section>
    <section><h2>Việc cần làm</h2>{urgent.length ? <div className={styles.techList}>{urgent.map((item) => <article className={styles.techCard} key={item.ticketId}><div><Link to={`/tickets/${item.ticketId}`}><strong>{item.ticketCode}</strong></Link><span>{item.deviceLabel} · {item.statusLabel}</span></div><SLAStatusBadge status={item.slaStatus} /><strong className={styles.largeCountdown}><SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} /></strong><SLAProgressBar startedAt={item.startedAt} dueAt={item.dueAt} slaStatus={item.slaStatus} /><Link to={`/tickets/${item.ticketId}`}>Mở phiếu để cập nhật →</Link></article>)}</div> : <EmptyState message="Bạn chưa có phiếu nào cần theo dõi SLA." />}</section>
    <section className={styles.card}><h2>Đang chờ linh kiện</h2>{waitingParts.length ? waitingParts.map((item) => <p key={item.ticketId}><Link to={`/tickets/${item.ticketId}`}>{item.ticketCode}</Link> — đồng hồ SLA {item.clockRunning ? 'đang chạy' : 'đang tạm dừng'} ({<SLACountdown dueAt={item.dueAt} slaStatus={item.slaStatus} />})</p>) : <EmptyState message="Không có phiếu đang chờ linh kiện." />}</section>
    <details className={styles.card}><summary>Đã hoàn tất gần đây ({data.recentCompleted.length})</summary>{data.recentCompleted.map((item) => <p key={item.ticket.ticketId}><Link to={`/tickets/${item.ticket.ticketId}`}>{item.ticket.ticketCode}</Link> — {item.completedOnTime ? 'Đúng hạn' : 'Hoàn tất trễ'}</p>)}</details>
  </main>;
}
