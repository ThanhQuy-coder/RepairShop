import { useEffect, useState } from 'react';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import { Button, ErrorMessage } from '../../components/common';
import { extractApiError } from '../../utils/apiError';
import styles from './MyAppointmentsPage.module.css';

const getStatusClass = (status: string) => {
  switch (status) {
    case 'Pending':
      return styles.statusPending;
    case 'Confirmed':
      return styles.statusConfirmed;
    case 'Cancelled':
      return styles.statusCancelled;
    case 'Completed':
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
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    try {
      setAppointments(await appointmentService.getMine());
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const cancel = async (id: string, lookupToken: string, phone: string) => {
    try {
      await appointmentService.cancel(id, lookupToken, phone);
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  return (
    <section className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Lịch hẹn của tôi</h1>
        <p className={styles.subtitle}>Quản lý và theo dõi các lịch hẹn khám của bạn.</p>
      </header>

      {error && <ErrorMessage message={error} onRetry={() => void load()} />}

      {appointments.length === 0 && !error && <p className={styles.empty}>Chưa có lịch hẹn.</p>}

      <div className={styles.list}>
        {appointments.map((appointment) => (
          <article key={appointment.id} className={styles.card}>
            <div className={styles.cardHeader}>
              <div className={styles.cardTitleGroup}>
                <strong className={styles.date}>{formatDate(appointment.appointmentDate)}</strong>
                <span className={styles.customerName}>{appointment.fullName}</span>
              </div>
              <span className={`${styles.status} ${getStatusClass(appointment.status)}`}>
                {appointment.status}
              </span>
            </div>

            <div className={styles.cardBody}>
              <p className={styles.info}>
                <span className={styles.infoLabel}>Số điện thoại:</span>
                {appointment.phone}
              </p>

              <p className={styles.info}>
                <span className={styles.infoLabel}>Khung giờ:</span>
                {appointment.slotStart} – {appointment.slotEnd}
              </p>

              <p className={styles.info}>
                <span className={styles.infoLabel}>Đặt lúc:</span>
                {formatDateTime(appointment.createdAt)}
              </p>

              {appointment.lookupToken && (
                <p className={styles.info}>
                  <span className={styles.infoLabel}>Mã tra cứu:</span>
                  <code className={styles.token}>{appointment.lookupToken}</code>
                </p>
              )}
            </div>

            {(appointment.status === 'Pending' || appointment.status === 'Confirmed') && (
              <div className={styles.actions}>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    void cancel(appointment.id, appointment.lookupToken!, appointment.phone)
                  }
                >
                  Hủy lịch hẹn
                </Button>
              </div>
            )}
          </article>
        ))}
      </div>
    </section>
  );
}
