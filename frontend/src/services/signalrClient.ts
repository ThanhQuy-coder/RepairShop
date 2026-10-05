import * as signalR from '@microsoft/signalr';
import { useNotificationStore } from '../store/notificationStore';
import type { NotificationItem } from '../types/notification.types';

let connection: signalR.HubConnection | null = null;

export function connectNotificationHub() {
  const token = localStorage.getItem('accessToken');
  if (!token || connection) return;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_BASE_URL.replace('/api', '')}/hubs/notifications`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect() // NFR-024: tự reconnect trong vòng 10s nếu mất kết nối
    .build();

  connection.on('ReceiveNotification', (payload: NotificationItem) => {
    const store = useNotificationStore.getState();
    store.setItems([payload, ...store.items]);
    store.setUnreadCount(store.unreadCount + 1);
  });

  connection.start().catch(() => {
    // Kết nối SignalR thất bại -> KHÔNG chặn ứng dụng, Bell vẫn hoạt động qua polling (Task 11.2)
  });
}

export function disconnectNotificationHub() {
  connection?.stop();
  connection = null;
}
