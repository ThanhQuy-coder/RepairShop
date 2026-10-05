import type { NotificationItem as NotificationItemType } from '../../types/notification.types';
import styles from './NotificationItem.module.css';

interface Props {
  notification: NotificationItemType;
  onClick: (notification: NotificationItemType) => void;
}

function timeAgo(dateString: string): string {
  const diffMs = Date.now() - new Date(dateString).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return 'Vừa xong';
  if (minutes < 60) return `${minutes} phút trước`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} ngày trước`;
  return new Date(dateString).toLocaleDateString('vi-VN');
}

export default function NotificationItem({ notification, onClick }: Props) {
  return (
    <div
      className={`${styles.item} ${!notification.isRead ? styles.unread : ''}`}
      onClick={() => onClick(notification)}
    >
      {!notification.isRead && <span className={styles.dot} />}
      <div className={styles.content}>
        <p className={styles.title}>{notification.title}</p>
        <p className={styles.message}>{notification.message}</p>
        <span className={styles.time}>{timeAgo(notification.createdAt)}</span>
      </div>
    </div>
  );
}
