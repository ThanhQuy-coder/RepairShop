import { Badge } from '../common';
import { TICKET_STATUS_LABELS, TICKET_STATUS_BADGE_VARIANT } from '../../constants/ticketStatus';

interface TicketStatusBadgeProps {
  status: string;
}

export default function TicketStatusBadge({ status }: TicketStatusBadgeProps) {
  return (
    <Badge variant={TICKET_STATUS_BADGE_VARIANT[status] ?? 'default'}>
      {TICKET_STATUS_LABELS[status] ?? status}
    </Badge>
  );
}
