import { create } from 'zustand';
import type { NotificationItem } from '../types/notification.types';

interface NotificationState {
  items: NotificationItem[];
  unreadCount: number;
  setItems: (items: NotificationItem[]) => void;
  setUnreadCount: (count: number) => void;
  markOneReadLocally: (id: string) => void;
  markAllReadLocally: () => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  items: [],
  unreadCount: 0,

  setItems: (items) => set({ items }),
  setUnreadCount: (count) => set({ unreadCount: count }),

  markOneReadLocally: (id) =>
    set((state) => ({
      items: state.items.map((n) =>
        n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n
      ),
      unreadCount: Math.max(0, state.unreadCount - 1),
    })),

  markAllReadLocally: () =>
    set((state) => ({
      items: state.items.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() })),
      unreadCount: 0,
    })),
}));
