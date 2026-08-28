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
