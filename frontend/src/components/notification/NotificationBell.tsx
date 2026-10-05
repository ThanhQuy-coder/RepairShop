import { useEffect, useRef, useState } from 'react';
import { notificationService } from '../../services/notificationService';
import { useNotificationStore } from '../../store/notificationStore';
import NotificationList from './NotificationList';
import styles from './NotificationBell.module.css';

const POLL_INTERVAL_MS = 30000; // polling tạm thời cho unread count — SignalR (Task 11.3) sẽ thay thế push real-time sau

export default function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const { unreadCount, setItems, setUnreadCount } = useNotificationStore();
  const wrapperRef = useRef<HTMLDivElement>(null);

  const fetchUnreadCount = () => {
    notificationService
      .getUnreadCount()
      .then(setUnreadCount)
      .catch(() => {});
  };

  useEffect(() => {
    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) setIsOpen(false);
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleToggle = () => {
    const next = !isOpen;
    setIsOpen(next);
    if (next) {
      setIsLoading(true);
      notificationService
        .list({ page: 1, pageSize: 20 })
        .then((res) => {
          setItems(res.items);
          setUnreadCount(res.unreadCount);
        })
        .finally(() => setIsLoading(false));
    }
  };

  return (
    <div className={styles.wrapper} ref={wrapperRef}>
      <button className={styles.bellButton} onClick={handleToggle} aria-label="Thông báo">
        🔔
        {unreadCount > 0 && (
          <span className={styles.badge}>{unreadCount > 99 ? '99+' : unreadCount}</span>
        )}
      </button>

      {isOpen && <NotificationList isLoading={isLoading} onClose={() => setIsOpen(false)} />}
    </div>
  );
}
