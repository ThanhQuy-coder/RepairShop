import { MAIN_TIMELINE_SEQUENCE, TICKET_STATUS_LABELS } from '../../constants/ticketStatus';
import styles from './PublicTrackingTimeline.module.css';

interface PublicStatusHistoryItem {
  status: string;
  statusLabel: string;
  changedAt: string;
}

interface PublicTrackingTimelineProps {
  currentStatus: string;
  statusHistory: PublicStatusHistoryItem[];
}

export default function PublicTrackingTimeline({ currentStatus, statusHistory }: PublicTrackingTimelineProps) {
  const reachedStatuses = new Set(statusHistory.map((h) => h.status));
  const currentIndex = MAIN_TIMELINE_SEQUENCE.indexOf(currentStatus);
  const isBranchStatus = !MAIN_TIMELINE_SEQUENCE.includes(currentStatus);

  return (
    <div>
      {isBranchStatus && (
        <div className={styles.branchBanner}>
          {currentStatus === 'CLOSED_REJECTED'
            ? 'Yêu cầu sửa chữa đã được đóng do báo giá bị từ chối.'
            : `Trạng thái hiện tại: ${TICKET_STATUS_LABELS[currentStatus] ?? currentStatus}`}
        </div>
      )}

      <div className={styles.timeline}>
        {MAIN_TIMELINE_SEQUENCE.map((code, index) => {
          const isDone = reachedStatuses.has(code) && index < (currentIndex === -1 ? MAIN_TIMELINE_SEQUENCE.length : currentIndex);
          const isCurrent = code === currentStatus;

          return (
            <div key={code} className={styles.step}>
              <div className={styles.markerCol}>
                <span className={`${styles.marker} ${isDone ? styles.markerDone : ''} ${isCurrent ? styles.markerCurrent : ''}`}>
                  {isDone ? '✓' : isCurrent ? '●' : '○'}
                </span>
                {index < MAIN_TIMELINE_SEQUENCE.length - 1 && (
                  <span className={`${styles.connector} ${isDone ? styles.connectorDone : ''}`} />
                )}
              </div>
              <span className={`${styles.label} ${isCurrent ? styles.labelCurrent : ''}`}>
                {TICKET_STATUS_LABELS[code]}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}