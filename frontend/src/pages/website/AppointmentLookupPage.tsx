import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, ErrorMessage, Input } from '../../components/common';
import {
  appointmentService,
  type AppointmentDetailResponse,
} from '../../services/appointmentService';
import styles from './AppointmentLookupPage.module.css';
import { extractApiError } from '../../utils/apiError';

const storageKey = 'appointment-lookup-token';

export default function AppointmentLookupPage() {
  const [token, setToken] = useState('');
  const [phone, setPhone] = useState('');
  const [result, setResult] = useState<AppointmentDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  useEffect(() => {
    try {
      const stored = JSON.parse(localStorage.getItem(storageKey) || 'null') as {
        token?: string;
        expiresAt?: number;
      } | null;
      if (stored?.token && stored.expiresAt && stored.expiresAt > Date.now())
        setToken(stored.token);
      else localStorage.removeItem(storageKey);
    } catch {
      localStorage.removeItem(storageKey);
    }
  }, []);

  const cancel = async (id: string, lookupToken: string, phone: string) => {
    setIsCancelling(true);
    setCancelError(null);

    try {
      if (window.confirm('Bạn chắc chắn muốn huỷ lịch hẹn này?')) {
        await appointmentService.cancel(id, lookupToken, phone);
        setResult(await appointmentService.lookup(lookupToken, phone));
      }
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      setResult(await appointmentService.lookup(token, phone));
    } catch (err) {
      const apiErr = extractApiError(err);
      setError(
        apiErr.status === 404
          ? 'Không tìm thấy lịch hẹn với thông tin đã cung cấp.'
          : apiErr.message
      );
    }
  };

  return (
    <main className={styles.page}>
      <h1 className={styles.title}>Tra cứu lịch hẹn</h1>
      <form className={styles.form} onSubmit={submit}>
        {error && <ErrorMessage message={error} />}
        <Input
          label="Mã tra cứu"
          value={token}
          onChange={(event) => setToken(event.target.value)}
          required
        />
        <Input
          label="Số điện thoại"
          type="tel"
          value={phone}
          onChange={(event) => setPhone(event.target.value)}
          required
        />
        <Button type="submit" className={styles.submit}>
          Tra cứu
        </Button>
      </form>
      {result && (
        <section className={styles.result}>
          <h2 className={styles.resultTitle}>Lịch hẹn {result.id}</h2>
          <p>Quý khách vui lòng đến đúng giờ đã hẹn</p>
          <div className={styles.resultRow}>
            <span className={styles.resultLabel}>Ngày</span>
            <span className={styles.resultValue}>
              {new Date(result.appointmentDate).toLocaleDateString('vi-VN', {
                weekday: 'long',
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
              })}
            </span>
          </div>
          <div className={styles.resultRow}>
            <span className={styles.resultLabel}>Thời gian</span>
            <span className={styles.resultValue}>
              {result.slotStart} - {result.slotEnd}
            </span>
          </div>
          <div className={styles.resultRow}>
            <span className={styles.resultLabel}>Thiết bị</span>
            <span className={styles.resultValue}>
              {[result.deviceType, result.brand, result.model, result.serialNumber]
                .filter(Boolean)
                .join(' - ') || 'Chưa cung cấp'}
            </span>
          </div>
          <div className={styles.resultRow}>
            <span className={styles.resultLabel}>Trạng thái cuộc hẹn</span>
            <span className={styles.resultValue}>{result.status}</span>
          </div>
          {result.status === 'Rejected' && result.rejectionReason && (
            <div className={styles.resultRow}>
              <span className={styles.resultLabel}>Lý do từ chối</span>
              <span className={styles.resultValue}>{result.rejectionReason}</span>
            </div>
          )}
          {result.status === 'Pending' && (
            <div className={styles.resultRow}>
              <span className={styles.resultLabelRed}>Hủy lịch hẹn</span>
              <Button
                variant="danger"
                size="sm"
                isLoading={isCancelling}
                onClick={() => void cancel(result.id, token, phone)}
              >
                Hủy
              </Button>
              {cancelError && <ErrorMessage message={cancelError} />}
            </div>
          )}
        </section>
      )}
    </main>
  );
}
