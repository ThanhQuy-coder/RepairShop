import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import { Button, ConfirmDialog, ErrorMessage, Loading } from '../../components/common';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import styles from './MyAppointmentsPage.module.css';

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Arrived: 'Đã đến cửa hàng',
  Completed: 'Đã hoàn thành',
  Complete: 'Đã hoàn thành',
  ConvertedToTicket: 'Đã tiếp nhận sửa chữa',
  Cancelled: 'Đã hủy',
  Rejected: 'Từ chối lịch hẹn',
  NoShow: 'Vắng mặt',
};

const TYPE_LABELS: Record<string, string> = {
  Repair: '🔧 Đặt lịch sửa chữa',
  Pickup: '📦 Nhận lại thiết bị',
  WarrantyRevisit: '🛡️ Bảo hành thiết bị',
};

const getStatusClass = (status: string) => {
  switch (status) {
    case 'Pending':
      return styles.statusPending;
    case 'Confirmed':
    case 'Arrived':
      return styles.statusConfirmed;
    case 'Cancelled':
    case 'Rejected':
    case 'NoShow':
      return styles.statusCancelled;
    case 'Completed':
    case 'Complete':
    case 'ConvertedToTicket':
      return styles.statusCompleted;
    default:
      return styles.statusDefault;
  }
};

const formatDate = (iso: string) => {
  try {
    return new Date(iso).toLocaleDateString('vi-VN', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  } catch {
    return iso;
  }
};

const formatDateTime = (iso: string) => {
  try {
    return new Date(iso).toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
};

export default function MyAppointmentsPage() {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterTab, setFilterTab] = useState<'all' | 'upcoming' | 'done' | 'cancelled'>('all');
  const [cancellingTarget, setCancellingTarget] = useState<AppointmentResponse | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);

  const load = async () => {
    setIsLoading(true);
    try {
      setAppointments(await appointmentService.getMine());
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const confirmCancel = async () => {
    if (!cancellingTarget) return;
    setIsCancelling(true);
    try {
      await appointmentService.cancel(
        cancellingTarget.id,
        cancellingTarget.lookupToken || '',
        cancellingTarget.phone
      );
      showSuccess('Đã hủy lịch hẹn thành công.');
      setCancellingTarget(null);
      await load();
    } catch (err) {
      showError(extractApiError(err).message);
    } finally {
      setIsCancelling(false);
    }
  };

  const stats = useMemo(() => {
    const upcoming = appointments.filter((a) =>
      ['Pending', 'Confirmed', 'Arrived'].includes(a.status)
    ).length;
    const done = appointments.filter((a) =>
      ['Completed', 'Complete', 'ConvertedToTicket'].includes(a.status)
    ).length;
    const cancelled = appointments.filter((a) =>
      ['Cancelled', 'Rejected', 'NoShow'].includes(a.status)
    ).length;
    return { total: appointments.length, upcoming, done, cancelled };
  }, [appointments]);

  const filteredAppointments = useMemo(() => {
    return appointments.filter((a) => {
      if (filterTab === 'upcoming') {
        return ['Pending', 'Confirmed', 'Arrived'].includes(a.status);
      }
      if (filterTab === 'done') {
        return ['Completed', 'Complete', 'ConvertedToTicket'].includes(a.status);
      }
      if (filterTab === 'cancelled') {
        return ['Cancelled', 'Rejected', 'NoShow'].includes(a.status);
      }
      return true;
    });
  }, [appointments, filterTab]);

  if (isLoading) return <Loading />;

  return (
    <section className={styles.page}>
      <header className={styles.headerRow}>
        <div>
          <span className={styles.eyebrow}>ĐẶT LỊCH TRỰC TUYẾN — FIXORA</span>
          <h1 className={styles.title}>Lịch hẹn của tôi</h1>
          <p className={styles.subtitle}>
            Quản lý thời gian hẹn kiểm tra, sửa chữa hoặc nhận thiết bị tại cửa hàng Fixora.
          </p>
        </div>
        <Button onClick={() => navigate('/book')}>+ Đặt lịch hẹn mới</Button>
      </header>

      {error && <ErrorMessage message={error} onRetry={() => void load()} />}

      {appointments.length === 0 && !error ? (
        <div className={styles.empty}>
          <p>Bạn chưa có lịch hẹn nào tại Fixora.</p>
          <Button variant="secondary" onClick={() => navigate('/book')}>
            Đặt lịch sửa chữa ngay
          </Button>
        </div>
      ) : (
        <>
          <div className={styles.filterTabs}>
            <button
              type="button"
              className={`${styles.filterTab} ${filterTab === 'all' ? styles.filterTabActive : ''}`}
              onClick={() => setFilterTab('all')}
            >
              <strong>{stats.total}</strong>
              <span>Tất cả lịch hẹn</span>
            </button>
            <button
              type="button"
              className={`${styles.filterTab} ${
                filterTab === 'upcoming' ? styles.filterTabActive : ''
              }`}
              onClick={() => setFilterTab('upcoming')}
            >
              <strong>{stats.upcoming}</strong>
              <span>Sắp tới / Chờ xử lý</span>
            </button>
            <button
              type="button"
              className={`${styles.filterTab} ${filterTab === 'done' ? styles.filterTabActive : ''}`}
              onClick={() => setFilterTab('done')}
            >
              <strong>{stats.done}</strong>
              <span>Đã tiếp nhận / Hoàn thành</span>
            </button>
            <button
              type="button"
              className={`${styles.filterTab} ${
                filterTab === 'cancelled' ? styles.filterTabActive : ''
              }`}
              onClick={() => setFilterTab('cancelled')}
            >
              <strong>{stats.cancelled}</strong>
              <span>Đã hủy / Từ chối</span>
            </button>
          </div>

          {filteredAppointments.length === 0 ? (
            <div className={styles.empty}>
              <p>Không có lịch hẹn nào trong mục này.</p>
            </div>
          ) : (
            <div className={styles.list}>
              {filteredAppointments.map((appointment) => (
                <article key={appointment.id} className={styles.card}>
                  <div className={styles.cardHeader}>
                    <div className={styles.cardTitleGroup}>
                      <div className={styles.dateBadgeRow}>
                        <span className={styles.typePill}>
                          {TYPE_LABELS[appointment.type] ?? appointment.type}
                        </span>
                        <strong className={styles.date}>
                          📅 {formatDate(appointment.appointmentDate)}
                        </strong>
                      </div>
                      <span className={styles.customerName}>
                        Khách hàng: <strong>{appointment.fullName}</strong> · 📞 {appointment.phone}
                      </span>
                    </div>
                    <span className={`${styles.status} ${getStatusClass(appointment.status)}`}>
                      {STATUS_LABELS[appointment.status] ?? appointment.status}
                    </span>
                  </div>

                  <div className={styles.cardBodyGrid}>
                    <div className={styles.infoBox}>
                      <span className={styles.infoLabel}>🕒 Khung giờ hẹn</span>
                      <strong>
                        {appointment.slotStart} – {appointment.slotEnd}
                      </strong>
                    </div>

                    <div className={styles.infoBox}>
                      <span className={styles.infoLabel}>📝 Thời điểm đặt lịch</span>
                      <strong>{formatDateTime(appointment.createdAt)}</strong>
                    </div>

                    {appointment.lookupToken && (
                      <div className={styles.infoBox}>
                        <span className={styles.infoLabel}>🔑 Mã tra cứu nhanh</span>
                        <code className={styles.token}>{appointment.lookupToken}</code>
                      </div>
                    )}
                  </div>

                  {(appointment.status === 'Pending' || appointment.status === 'Confirmed') && (
                    <div className={styles.actions}>
                      <span className={styles.cancelHint}>
                        Bạn có thể hủy lịch hẹn miễn phí trước giờ hẹn.
                      </span>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setCancellingTarget(appointment)}
                      >
                        Hủy lịch hẹn
                      </Button>
                    </div>
                  )}
                </article>
              ))}
            </div>
          )}
        </>
      )}

      <ConfirmDialog
        isOpen={!!cancellingTarget}
        title="Xác nhận hủy lịch hẹn"
        message={
          cancellingTarget
            ? `Bạn có chắc chắn muốn hủy lịch hẹn ngày ${new Date(
                cancellingTarget.appointmentDate
              ).toLocaleDateString('vi-VN')} (${cancellingTarget.slotStart} - ${
                cancellingTarget.slotEnd
              }) không?`
            : ''
        }
        confirmLabel="Xác nhận hủy lịch"
        cancelLabel="Giữ lịch hẹn"
        isDangerous
        isLoading={isCancelling}
        onConfirm={() => void confirmCancel()}
        onCancel={() => setCancellingTarget(null)}
      />
    </section>
  );
}
