import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Badge, Button, ErrorMessage, Loading } from '../../components/common';
import {
  appointmentService,
  type AppointmentDetailResponse,
} from '../../services/appointmentService';
import styles from '../customers/CustomerDetailPage.module.css';

const statusLabels: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Rejected: 'Đã từ chối',
  Cancelled: 'Đã hủy',
  Converted: 'Đã chuyển ticket',
  NoShow: 'Không đến',
};

const statusVariants: Record<string, 'default' | 'success' | 'warning' | 'danger' | 'info'> = {
  Pending: 'warning',
  Confirmed: 'success',
  Rejected: 'danger',
  Cancelled: 'danger',
  Converted: 'info',
  NoShow: 'default',
};

export default function AppointmentDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [appointment, setAppointment] = useState<AppointmentDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    appointmentService
      .getById(id)
      .then(setAppointment)
      .catch(() => setError('Không thể tải lịch hẹn hoặc lịch hẹn không tồn tại.'));
  }, [id]);

  if (error)
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Button variant="ghost" size="sm" onClick={() => navigate('/staff/dashboard')}>
            ← Quay lại
          </Button>
        </div>
        <ErrorMessage message={error} />
      </div>
    );
  if (!appointment) return <Loading message="Đang tải lịch hẹn..." />;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Button variant="ghost" size="sm" onClick={() => navigate('/staff/dashboard')}>
          ← Danh sách lịch hẹn
        </Button>
      </div>

      <section className={styles.section}>
        <h3>Chi tiết lịch hẹn</h3>
        <div className={styles.infoGrid}>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Khách hàng</span>
            <p className={styles.infoValue}>{appointment.fullName}</p>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Số điện thoại</span>
            <p className={styles.infoValue}>{appointment.phone}</p>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Ngày hẹn</span>
            <p className={styles.infoValue}>{appointment.appointmentDate}</p>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Trạng thái</span>
            <Badge variant={statusVariants[appointment.status] ?? 'default'}>
              {statusLabels[appointment.status] ?? appointment.status}
            </Badge>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Thời gian bắt đầu</span>
            <p className={styles.infoValue}>{appointment.slotStart}</p>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Thời gian kết thúc</span>
            <p className={styles.infoValue}>{appointment.slotEnd}</p>
          </div>
        </div>
      </section>

      <section className={styles.section}>
        <h3>Thông tin thiết bị và yêu cầu</h3>
        <div className={styles.infoGrid}>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Thiết bị</span>
            <p className={styles.infoValue}>
              {[appointment.deviceType, appointment.brand, appointment.model]
                .filter(Boolean)
                .join(' ') || 'Chưa cung cấp'}
            </p>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Mô tả vấn đề</span>
            <p className={styles.infoValue}>{appointment.issueDescription || 'Chưa cung cấp'}</p>
          </div>
        </div>
      </section>
      <section className={styles.section}>
        <h3>Lý do từ chối</h3>
        <div className={styles.infoGrid}>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Lý do</span>
            <p className={styles.infoValue}>{appointment.rejectionReason || 'Không có'}</p>
          </div>
        </div>
      </section>
    </div>
  );
}
