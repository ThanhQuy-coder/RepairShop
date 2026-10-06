import * as signalR from '@microsoft/signalr';
import { useNotificationStore } from '../store/notificationStore';
import type { NotificationItem } from '../types/notification.types';

const HUB_URL = `${import.meta.env.VITE_API_BASE_URL.replace(/\/api\/?$/, '')}/hubs/notifications`;

/**
 * NFR-024: tự reconnect trong vòng 10 giây. Mảng dưới đây là danh sách độ trễ (ms) giữa các lần
 * thử lại — lần đầu gần như ngay lập tức, các lần sau giãn ra dần nhưng KHÔNG vượt 9s cho lần
 * cuối trong cửa sổ 10 giây đầu, đảm bảo luôn có ít nhất 1 lần thử trong hạn.
 */
const RECONNECT_DELAYS_MS = [0, 1000, 3000, 6000, 9000];

class NotificationSignalRService {
  private connection: signalR.HubConnection | null = null;

  /** Connect — idempotent, gọi nhiều lần không tạo nhiều kết nối song song. */
  connect(): void {
    if (this.connection) return;

    const token = localStorage.getItem('accessToken');
    if (!token) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => localStorage.getItem('accessToken') ?? '' })
      .withAutomaticReconnect(RECONNECT_DELAYS_MS)
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    this.connection.start().catch(() => {
      // Kết nối thất bại lúc khởi động KHÔNG được chặn ứng dụng — Notification Center vẫn hoạt
      // động qua polling (NotificationBell, Task 11.2). SignalR chỉ là enhancement, không phải
      // dependency bắt buộc (đúng tinh thần sẽ kiểm chứng ở Task 11.6).
    });
  }

  /** Receive notification — cập nhật React state qua store dùng chung với phần polling. */
  private registerHandlers(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveNotification', (payload: NotificationItem) => {
      const store = useNotificationStore.getState();
      store.setItems([payload, ...store.items]);
      store.setUnreadCount(store.unreadCount + 1);
    });

    this.connection.onreconnecting(() => {
      // Tùy chọn: có thể hiện indicator "đang kết nối lại" — bỏ qua ở mức tối thiểu hiện tại
    });

    this.connection.onreconnected(() => {
      // Sau khi reconnect thành công, đồng bộ lại unread count thật từ Backend — phòng trường hợp
      // có notification phát sinh TRONG lúc mất kết nối mà client bỏ lỡ hoàn toàn (SignalR không
      // replay message cũ). Import động để tránh circular dependency với notificationService.
      import('./notificationService').then(({ notificationService }) => {
        notificationService
          .getUnreadCount()
          .then((count) => useNotificationStore.getState().setUnreadCount(count));
      });
    });
  }

  /** Disconnect cleanup — gọi khi logout, tránh rò rỉ kết nối/giữ group cũ trên server. */
  disconnect(): void {
    if (!this.connection) return;
    this.connection.stop();
    this.connection = null;
  }

  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const notificationSignalRService = new NotificationSignalRService();
