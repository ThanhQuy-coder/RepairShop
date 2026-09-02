// Đúng label tiếng Việt đã dùng ở Backend (Task 4.15 - StatusLabels)
export const TICKET_STATUS_LABELS: Record<string, string> = {
  CHECKED_IN: 'Đã tiếp nhận',
  ASSIGNED: 'Đã phân công kỹ thuật viên',
  DIAGNOSING: 'Đang kiểm tra',
  WAITING_APPROVAL: 'Chờ khách xác nhận báo giá',
  ON_HOLD: 'Tạm hoãn',
  WAITING_PARTS: 'Chờ linh kiện',
  IN_REPAIR: 'Đang sửa chữa',
  QA_TESTING: 'Đang kiểm thử',
  READY_FOR_PICKUP: 'Sẵn sàng bàn giao',
  DELIVERED: 'Đã bàn giao',
  CLOSED_REJECTED: 'Đã đóng (từ chối báo giá)',
};

export const TICKET_STATUS_BADGE_VARIANT: Record<
  string,
  'default' | 'success' | 'warning' | 'danger' | 'info'
> = {
  CHECKED_IN: 'info',
  ASSIGNED: 'info',
  DIAGNOSING: 'warning',
  WAITING_APPROVAL: 'warning',
  ON_HOLD: 'default',
  WAITING_PARTS: 'warning',
  IN_REPAIR: 'warning',
  QA_TESTING: 'warning',
  READY_FOR_PICKUP: 'success',
  DELIVERED: 'success',
  CLOSED_REJECTED: 'danger',
};

export const MAIN_TIMELINE_SEQUENCE = [
  'CHECKED_IN',
  'ASSIGNED',
  'DIAGNOSING',
  'WAITING_APPROVAL',
  'IN_REPAIR',
  'QA_TESTING',
  'READY_FOR_PICKUP',
  'DELIVERED',
];

export const STATUS_ICON: Record<string, string> = {
  CHECKED_IN: '📥',
  ASSIGNED: '👤',
  DIAGNOSING: '🔍',
  WAITING_APPROVAL: '📝',
  ON_HOLD: '⏸️',
  WAITING_PARTS: '📦',
  IN_REPAIR: '🔧',
  QA_TESTING: '🧪',
  READY_FOR_PICKUP: '✅',
  DELIVERED: '🎉',
  CLOSED_REJECTED: '❌',
};

// Gom 11 status thành 5 nhóm cho Dashboard tổng quan — khác với MAIN_TIMELINE_SEQUENCE (dùng cho
// Timeline hiển thị TỪNG bước tuần tự của 1 ticket). Ở đây cần độ "cô đọng" hơn để nhìn tổng quan
// toàn bộ cửa hàng đang có bao nhiêu việc ở mỗi giai đoạn.
export const DASHBOARD_STATUS_GROUPS: {
  key: string;
  label: string;
  statuses: string[];
  icon: string;
}[] = [
  {
    key: 'pending',
    label: 'Đang chờ xử lý',
    icon: '⏳',
    statuses: ['CHECKED_IN', 'ASSIGNED', 'DIAGNOSING', 'WAITING_APPROVAL', 'ON_HOLD'],
  },
  {
    key: 'inRepair',
    label: 'Đang sửa chữa',
    icon: '🔧',
    statuses: ['WAITING_PARTS', 'IN_REPAIR', 'QA_TESTING'],
  },
  { key: 'readyForPickup', label: 'Sẵn sàng bàn giao', icon: '✅', statuses: ['READY_FOR_PICKUP'] },
  { key: 'completed', label: 'Đã hoàn thành', icon: '🎉', statuses: ['DELIVERED'] },
  { key: 'closed', label: 'Đã đóng (từ chối)', icon: '❌', statuses: ['CLOSED_REJECTED'] },
];
