import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { PublicTicketTracking } from '../../types/ticket.types';
import { Loading, ErrorMessage } from '../../components/common';
import TrackSearchBox from '../../components/track/TrackSearchBox';
import PublicTrackingTimeline from '../../components/track/PublicTrackingTimeline';
import styles from './TrackTicketPage.module.css';
import { STATUS_ICON } from '../../constants/ticketStatus';

export default function TrackTicketPage() {
  const { ticketCode } = useParams<{ ticketCode: string }>();

  const [data, setData] = useState<PublicTicketTracking | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const retry = () => {
    if (!ticketCode) return;
    setIsLoading(true);
    setErrorMessage(null);
    ticketService
      .trackByCode(ticketCode)
      .then(setData)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(() => {
    if (!ticketCode) {
      setData(null);
      setErrorMessage(null);
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);
    ticketService
      .trackByCode(ticketCode)
      .then(setData)
      .catch((err) => {
        // Backend cố tình trả message CHUNG CHUNG cho mọi lỗi tra cứu (Task 4.15, Tuần 4) —
        // không tiết lộ "mã không tồn tại" khác với "mã tồn tại nhưng có lỗi khác", tránh dò mã hàng loạt.
        setErrorMessage(extractApiError(err).message);
        setData(null);
      })
      .finally(() => setIsLoading(false));
  }, [ticketCode]);

  // Chưa nhập mã — hiện form tìm kiếm
  if (!ticketCode) {
    return <TrackSearchBox />;
  }

  if (isLoading) return <Loading message="Đang tra cứu..." />;

  if (errorMessage || !data) {
    return (
      <div className={styles.wrapper}>
        <TrackSearchBox initialValue={ticketCode} />
        <ErrorMessage message={errorMessage ?? '...'} onRetry={retry} />{' '}
      </div>
    );
  }

  return (
    <div className={styles.wrapper}>
      <TrackSearchBox initialValue={ticketCode} />

      <div className={styles.resultCard}>
        <div className={styles.deviceHeader}>
          <span className={styles.deviceLabel}>{data.deviceLabel}</span>
          <span className={styles.ticketCode}>
            Mã phiếu: <strong>{data.ticketCode}</strong>
          </span>
        </div>

        <div className={styles.statusBanner}>
          <span className={styles.statusIcon}>{STATUS_ICON[data.status] ?? '📋'}</span>
          <span className={styles.statusText}>{data.statusLabel}</span>
        </div>

        <div className={styles.statusBanner}>
          <span className={styles.statusText}>{data.status}</span>
        </div>

        <PublicTrackingTimeline currentStatus={data.status} statusHistory={data.statusHistory} />

        {data.estimatedCompletion && (
          <p className={styles.estimate}>
            Dự kiến hoàn thành: {new Date(data.estimatedCompletion).toLocaleDateString('vi-VN')}
          </p>
        )}
      </div>
    </div>
  );
}
