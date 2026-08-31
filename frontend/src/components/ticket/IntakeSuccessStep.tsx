import { useNavigate } from 'react-router-dom';
import { Button, Badge } from '../common';
import type { Ticket } from '../../types/ticket.types';
import type { Customer } from '../../types/customer.types';
import type { Device } from '../../types/device.types';
import { TICKET_STATUS_LABELS } from '../../constants/ticketStatus';
import styles from './IntakeSuccessStep.module.css';

interface IntakeSuccessStepProps {
  ticket: Ticket;
  customer: Customer | null;
  device: Device | null;
}

export default function IntakeSuccessStep({ ticket, customer, device }: IntakeSuccessStepProps) {
  const navigate = useNavigate();

  const handlePrint = () => window.print();

  return (
    <div>
      <div className={styles.successBox}>
        <div className={styles.checkIcon}>✓</div>
        <h3>Tạo phiếu sửa chữa thành công</h3>

        <div className={styles.ticketCode}>{ticket.ticketCode}</div>
        <Badge variant="info">{TICKET_STATUS_LABELS[ticket.status]}</Badge>

        <div className={styles.actions}>
          <Button onClick={() => navigate(`/tickets/${ticket.id}`)}>Xem phiếu</Button>
          <Button variant="secondary" onClick={handlePrint}>
            In phiếu
          </Button>
          <Button variant="ghost" onClick={() => window.location.reload()}>
            Tiếp nhận phiếu mới
          </Button>
        </div>
      </div>

      {/* Khu vực chỉ hiện khi in (window.print) — ẩn khi xem bình thường trên màn hình */}
      <div className={styles.printOnly}>
        <h2>PHIẾU TIẾP NHẬN THIẾT BỊ</h2>
        <p>
          Mã phiếu: <strong>{ticket.ticketCode}</strong>
        </p>
        <p>
          Khách hàng: {customer?.fullName} — {customer?.phone}
        </p>
        <p>
          Thiết bị: {device?.brand} {device?.model}{' '}
          {device?.serialNumber ? `(${device.serialNumber})` : ''}
        </p>
        <p>Mô tả lỗi: {ticket.issueReported}</p>
        <p>Ngày nhận: {new Date(ticket.receivedAt).toLocaleString('vi-VN')}</p>
      </div>
    </div>
  );
}
