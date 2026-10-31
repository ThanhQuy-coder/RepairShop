import { useId, useState } from 'react';
import { type StatusHistoryItem } from '../../types/ticket.types';
import { MAIN_TIMELINE_SEQUENCE, TICKET_STATUS_LABELS } from '../../constants/ticketStatus';
import styles from './TicketTimeline.module.css';

interface TicketTimelineProps {
  currentStatus: string;
  statusHistory: StatusHistoryItem[];
}

export default function TicketTimeline({ currentStatus, statusHistory }: TicketTimelineProps) {
  const [isExpanded, setIsExpanded] = useState(false); // ← đổi thành true nếu muốn mở mặc định
  const bodyId = useId();

  const reachedStatuses = new Set(statusHistory.map((h) => h.toStatus));
  const currentIndex = MAIN_TIMELINE_SEQUENCE.indexOf(currentStatus);
  const isBranchStatus = !MAIN_TIMELINE_SEQUENCE.includes(currentStatus);

  return (
    <div className={styles.root}>
      {isBranchStatus && (
        <div className={styles.branchBanner}>
          Ticket hiện đang ở trạng thái đặc biệt:{' '}
          <strong>{TICKET_STATUS_LABELS[currentStatus] ?? currentStatus}</strong>
          {currentStatus === 'CLOSED_REJECTED' &&
            ' — quy trình đã kết thúc do khách từ chối báo giá.'}
        </div>
      )}

      <button
        type="button"
        className={styles.toggleButton}
        onClick={() => setIsExpanded((v) => !v)}
        aria-expanded={isExpanded}
        aria-controls={bodyId}
      >
        <span>{isExpanded ? 'Thu gọn quy trình' : 'Xem toàn bộ quy trình'}</span>
        <span className={styles.toggleChevron} aria-hidden="true">
          {isExpanded ? '▲' : '▼'}
        </span>
      </button>

      <div id={bodyId}>
        {isExpanded ? (
          <div className={styles.timelineScroll}>
            <div className={styles.timeline}>
              {MAIN_TIMELINE_SEQUENCE.map((code, index) => {
                const isDone =
                  reachedStatuses.has(code) &&
                  index < (currentIndex === -1 ? MAIN_TIMELINE_SEQUENCE.length : currentIndex);
                const isCurrent = code === currentStatus;
                const isPending = !isDone && !isCurrent;

                return (
                  <div key={code} className={styles.step}>
                    <div className={styles.stepMarkerCol}>
                      <span
                        className={`${styles.marker} ${isDone ? styles.markerDone : ''} ${isCurrent ? styles.markerCurrent : ''}`}
                      >
                        {isDone ? '✓' : isCurrent ? '●' : '○'}
                      </span>
                      {index < MAIN_TIMELINE_SEQUENCE.length - 1 && (
                        <span
                          className={`${styles.connector} ${isDone ? styles.connectorDone : ''}`}
                        />
                      )}
                    </div>
                    <span
                      className={`${styles.stepLabel} ${isCurrent ? styles.stepLabelCurrent : ''} ${isPending ? styles.stepLabelPending : ''}`}
                    >
                      {TICKET_STATUS_LABELS[code]}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        ) : (
          <div className={styles.compactStatus}>
            <span className={`${styles.marker} ${styles.markerCurrent}`} aria-hidden="true">
              ●
            </span>
            <span className={styles.compactLabel}>
              Đang ở: <strong>{TICKET_STATUS_LABELS[currentStatus] ?? currentStatus}</strong>
            </span>
          </div>
        )}
      </div>

      <details className={styles.historyDetails}>
        <summary>Xem lịch sử chi tiết ({statusHistory.length} thay đổi)</summary>
        <ul className={`${styles.historyList} ${styles.historyScroll}`}>
          {statusHistory.map((h, i) => (
            <li key={i}>
              <span className={styles.historyTransition}>
                {h.fromStatus ? `${TICKET_STATUS_LABELS[h.fromStatus] ?? h.fromStatus} → ` : ''}
                {TICKET_STATUS_LABELS[h.toStatus] ?? h.toStatus}
              </span>
              <span className={styles.historyMeta}>
                {h.changedByName} · {new Date(h.changedAt).toLocaleString('vi-VN')}
              </span>
              {h.note && <p className={styles.historyNote}>{h.note}</p>}
            </li>
          ))}
        </ul>
      </details>
    </div>
  );
}
