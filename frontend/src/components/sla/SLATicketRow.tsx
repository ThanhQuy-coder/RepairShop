import { Link } from 'react-router-dom';
import { Button } from '../common';
import type { SlaTicketItem } from '../../types/reports.types';
import SLAStatusBadge from './SLAStatusBadge';
import SLACountdown from './SLACountdown';
import SLAProgressBar from './SLAProgressBar';
import styles from './sla.module.css';

export default function SLATicketRow({ ticket, showAction = true }: { ticket: SlaTicketItem; showAction?: boolean }) {
  return <tr>
    <td><Link to={`/tickets/${ticket.ticketId}`}>{ticket.ticketCode}</Link></td>
    <td>{ticket.customerName}<br /><small>{ticket.deviceLabel}</small></td>
    <td>{ticket.statusLabel}</td>
    <td><SLAStatusBadge status={ticket.slaStatus} /><div className={styles.countdown}><SLACountdown dueAt={ticket.dueAt} slaStatus={ticket.slaStatus} /></div><SLAProgressBar startedAt={ticket.startedAt} dueAt={ticket.dueAt} slaStatus={ticket.slaStatus} /></td>
    <td>{ticket.technicianName ?? 'Chưa phân công'}</td>
    {showAction && <td><Button size="sm" variant="secondary" onClick={() => window.location.assign(`/tickets/${ticket.ticketId}`)}>Xem phiếu</Button></td>}
  </tr>;
}
