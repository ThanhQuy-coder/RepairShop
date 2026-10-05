import { useNavigate } from 'react-router-dom';
import type { NotificationItem as NotificationItemType } from '../../types/notification.types';
import { notificationService } from '../../services/notificationService';
import { useNotificationStore } from '../../store/notificationStore';
import { Button, Loading, EmptyState } from '../common';
import NotificationItem from './NotificationItem';
import styles from './NotificationList.module.css';

interface Props {
  isLoading: boolean;
  onClose: () => void;
}

// Map loại thông báo -> route điều hướng khi click, dễ mở rộng khi có thêm loại mới (SLA, Inventory...)
function resolveRoute(n: NotificationItemType): string | null {
  if (n.relatedEntityType === 'Appointment' && n.relatedEntityId)
    return `/staff/appointments/${n.relatedEntityId}`;
  if (n.relatedEntityType === 'Ticket' && n.relatedEntityId) return `/tickets/${n.relatedEntityId}`;
  return null;
}

export default function NotificationList({ isLoading, onClose }: Props) {
  const navigate = useNavigate();
  const { items, unreadCount, markOneReadLocally, markAllReadLocally } = useNotificationStore();

  const handleItemClick = async (notification: NotificationItemType) => {
    if (!notification.isRead) {
      markOneReadLocally(notification.id); // cập nhật UI ngay (optimistic), không chờ response
      notificationService.markAsRead(notification.id).catch(() => {});
    }

    const route = resolveRoute(notification);
    onClose();
    if (route) navigate(route);
  };

  const handleMarkAll = async () => {
    markAllReadLocally();
    await notificationService.markAllAsRead().catch(() => {});
  };

  return (
    <div className={styles.dropdown}>
      <div className={styles.header}>
        <span className={styles.headerTitle}>Thông báo</span>
        {unreadCount > 0 && (
          <Button variant="ghost" size="sm" onClick={handleMarkAll}>
            Đánh dấu tất cả đã đọc
          </Button>
        )}
      </div>

      <div className={styles.body}>
        {isLoading && <Loading message="Đang tải thông báo..." />}
        {!isLoading && items.length === 0 && <EmptyState message="Không có thông báo nào." />}
        {!isLoading &&
          items.map((n) => (
            <NotificationItem key={n.id} notification={n} onClick={handleItemClick} />
          ))}
      </div>
    </div>
  );
}
