import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import {
  Badge,
  Button,
  ErrorMessage,
  Modal,
  Pagination,
  Table,
  type TableColumn,
} from '../../components/common';
import { extractApiError } from '../../utils/apiError';
import type { AppointmentIntakeData } from '../../store/ticketIntakeStore';
import { useAuth } from '../../hooks/useAuth';
import { useToast } from '../../hooks/useToast';
import styles from './AppointmentManagementPage.module.css';

const statusLabels: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Rejected: 'Đã từ chối',
  Cancelled: 'Đã hủy',
  Arrived: 'Khách đã đến',
  Complete: 'Hoàn thành',
  Completed: 'Hoàn thành',
  ConvertedToTicket: 'Đã tạo phiếu',
  NoShow: 'Không đến',
};

const typeLabels: Record<string, string> = {
  Repair: 'Sửa chữa',
  Pickup: 'Nhận thiết bị',
  WarrantyRevisit: 'Bảo hành lại',
};

export default function AppointmentManagementPage() {
  const navigate = useNavigate();
  const currentUser = useAuth().user;
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rejectingAppointment, setRejectingAppointment] = useState<AppointmentResponse | null>(
    null
  );
  const [rejectionReason, setRejectionReason] = useState('');
  const [reasonError, setReasonError] = useState<string | null>(null);
  const [completingAppointment, setCompletingAppointment] = useState<AppointmentResponse | null>(
    null
  );
  const [isCompleting, setIsCompleting] = useState(false);

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [customerFilter, setCustomerFilter] = useState<'all' | 'account' | 'guest'>('all');
  const [processingWarrantyId, setProcessingWarrantyId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 10;
  const { showSuccess, showError } = useToast();

  const handleCreateTicket = async (data: AppointmentResponse) => {
    try {
      const detail = await appointmentService.getById(data.id);

      const appointment: AppointmentIntakeData = {
        id: detail.id,
        customerId: detail.customerId,
        fullName: detail.fullName,
        phone: detail.phone,
        appointmentDate: detail.appointmentDate,
        timeSlotId: detail.timeSlotId,
        slotStart: detail.slotStart,
        slotEnd: detail.slotEnd,
        status: detail.status,
        createdAt: detail.createdAt,
        lookupToken: detail.lookupToken,
        deviceType: detail.deviceType as
          | 'Phone'
          | 'Laptop'
          | 'Electronics'
          | 'Tablet'
          | 'Smartwatch',
        brand: detail.brand,
        model: detail.model,
        serialNumber: detail.serialNumber,
      };

      navigate('/tickets/create', {
        state: { appointment },
      });
    } catch (err) {
      showError(extractApiError(err).message);
    }
  };

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      setAppointments(await appointmentService.getForStaff());
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [searchQuery, statusFilter, typeFilter, customerFilter]);

  const transition = async (
    id: string,
    reason: string,
    action: 'confirm' | 'reject' | 'noShow' | 'markCompleted' | 'markArrived'
  ) => {
    try {
      await appointmentService[action](id, reason);
      showSuccess('Cập nhật trạng thái lịch hẹn thành công.');
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  const handleOpenRejectModal = (appointment: AppointmentResponse) => {
    setRejectingAppointment(appointment);
    setRejectionReason('');
    setReasonError(null);
  };

  const handleConfirmReject = async () => {
    if (!rejectionReason.trim()) {
      setReasonError('Vui lòng nhập lý do từ chối.');
      return;
    }

    if (!rejectingAppointment) return;

    await transition(rejectingAppointment.id, rejectionReason.trim(), 'reject');
    setRejectingAppointment(null);
  };

  const handleOpenCompleteModal = (appointment: AppointmentResponse) => {
    setCompletingAppointment(appointment);
  };

  const handleConfirmComplete = async () => {
    if (!completingAppointment) return;
    setIsCompleting(true);
    try {
      await transition(completingAppointment.id, '', 'markCompleted');
      setCompletingAppointment(null);
    } finally {
      setIsCompleting(false);
    }
  };

  const handleReceiveWarranty = async (appointmentId: string) => {
    setProcessingWarrantyId(appointmentId);
    try {
      await appointmentService.convertToTicket(appointmentId);
      showSuccess('Đã tiếp nhận bảo hành và tạo phiếu sửa chữa thành công.');
      await load();
    } catch (err) {
      showError(extractApiError(err).message);
      setError(extractApiError(err).message);
    } finally {
      setProcessingWarrantyId(null);
    }
  };

  const statusCounts = useMemo(() => {
    return {
      all: appointments.length,
      Pending: appointments.filter((a) => a.status === 'Pending').length,
      Confirmed: appointments.filter((a) => a.status === 'Confirmed').length,
      Arrived: appointments.filter((a) => a.status === 'Arrived').length,
      Completed: appointments.filter(
        (a) =>
          a.status === 'Completed' ||
          a.status === 'Complete' ||
          a.status === 'ConvertedToTicket'
      ).length,
    };
  }, [appointments]);

  const columns: TableColumn<AppointmentResponse>[] = [
    {
      key: 'date',
      header: 'Ngày & Khung giờ',
      render: (a) => (
        <div className={styles.dateCell}>
          <span className={styles.dateMain}>
            📅 {new Date(a.appointmentDate).toLocaleDateString('vi-VN')}
          </span>
          <span className={styles.slotSub}>
            🕒 {a.slotStart} - {a.slotEnd}
          </span>
        </div>
      ),
    },
    {
      key: 'type',
      header: 'Loại hẹn',
      render: (a) => (
        <span className={styles.typeTag} data-type={a.type}>
          {typeLabels[a.type] ?? a.type}
        </span>
      ),
    },
    {
      key: 'name',
      header: 'Khách hàng',
      render: (a) => (
        <div className={styles.customerCell}>
          <strong>{a.fullName}</strong>
          <div className={styles.customerSub}>
            <span>📞 {a.phone}</span>
            {a.customerId ? (
              <span className={styles.accountPill}>Thành viên</span>
            ) : (
              <span className={styles.guestPill}>Khách vãng lai</span>
            )}
          </div>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (a) => (
        <Badge
          variant={
            a.status === 'Pending'
              ? 'warning'
              : a.status === 'Confirmed' || a.status === 'Arrived'
                ? 'info'
                : a.status === 'Rejected' || a.status === 'NoShow' || a.status === 'Cancelled'
                  ? 'danger'
                  : 'success'
          }
        >
          {statusLabels[a.status] ?? a.status}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác điều phối',
      render: (a) => (
        <div
          className={styles.actionBtnGroup}
          onClick={(event) => event.stopPropagation()}
        >
          {a.status === 'Pending' && (
            <>
              <Button size="sm" onClick={() => void transition(a.id, '', 'confirm')}>
                ✓ Xác nhận
              </Button>
              <Button size="sm" variant="danger" onClick={() => handleOpenRejectModal(a)}>
                ✕ Từ chối
              </Button>
            </>
          )}
          {a.status === 'Confirmed' && (
            <>
              <Button size="sm" onClick={() => void transition(a.id, '', 'markArrived')}>
                📍 Khách đã đến
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={() => void transition(a.id, 'Khách không đến theo lịch hẹn', 'noShow')}
              >
                Vắng mặt
              </Button>
            </>
          )}
          {a.status === 'Arrived' &&
            (a.handledByUserId === currentUser?.id || a.handledByUserId === null) && (
              <>
                {a.type === 'WarrantyRevisit' ? (
                  <Button
                    size="sm"
                    onClick={() => void handleReceiveWarranty(a.id)}
                    disabled={processingWarrantyId === a.id}
                    isLoading={processingWarrantyId === a.id}
                  >
                    🛡️ Tiếp nhận BH
                  </Button>
                ) : (
                  <>
                    <Button size="sm" onClick={() => void handleCreateTicket(a)}>
                      + Tạo phiếu sửa
                    </Button>
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => handleOpenCompleteModal(a)}
                    >
                      ✓ Hoàn thành
                    </Button>
                  </>
                )}
              </>
            )}
          <Button
            size="sm"
            variant="ghost"
            onClick={() => navigate(`/staff/appointments/${a.id}`)}
          >
            Chi tiết →
          </Button>
        </div>
      ),
    },
  ];

  const filteredAppointments = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return appointments.filter((a) => {
      if (statusFilter) {
        if (statusFilter === 'CompletedGroup') {
          if (
            a.status !== 'Completed' &&
            a.status !== 'Complete' &&
            a.status !== 'ConvertedToTicket'
          ) {
            return false;
          }
        } else if (a.status !== statusFilter) {
          return false;
        }
      }
      if (typeFilter && a.type !== typeFilter) return false;
      if (customerFilter !== 'all') {
        const hasAccount = a.customerId !== null;
        if (customerFilter === 'account' && !hasAccount) return false;
        if (customerFilter === 'guest' && hasAccount) return false;
      }
      if (q) {
        const matchName = a.fullName.toLowerCase().includes(q);
        const matchPhone = a.phone.toLowerCase().includes(q);
        return matchName || matchPhone;
      }
      return true;
    });
  }, [appointments, searchQuery, statusFilter, typeFilter, customerFilter]);

  const pagedAppointments = filteredAppointments.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>ĐIỀU PHỐI LỊCH HẸN — FIXORA</span>
          <h1 className={styles.title}>Quản lý Lịch hẹn Khách hàng</h1>
          <p className={styles.subtitle}>
            Xác nhận lịch đặt trước, check-in khi khách đến cửa hàng và chuyển đổi nhanh thành phiếu sửa chữa.
          </p>
        </div>
        <div className={styles.headerActions}>
          <Button variant="secondary" size="md" onClick={() => void load()}>
            ↻ Làm mới
          </Button>
          <Button size="md" onClick={() => navigate('/tickets/create')}>
            + Tiếp nhận trực tiếp
          </Button>
        </div>
      </div>

      {/* Thẻ KPI lọc nhanh theo trạng thái lịch hẹn */}
      <div className={styles.statusTabs}>
        <button
          type="button"
          className={`${styles.statusTab} ${statusFilter === '' ? styles.statusTabActive : ''}`}
          onClick={() => setStatusFilter('')}
        >
          <strong>{statusCounts.all}</strong>
          <span>Tất cả lịch hẹn</span>
        </button>
        <button
          type="button"
          className={`${styles.statusTab} ${
            statusFilter === 'Pending' ? styles.statusTabActive : ''
          }`}
          onClick={() => setStatusFilter('Pending')}
        >
          <strong>{statusCounts.Pending}</strong>
          <span>Chờ xác nhận</span>
        </button>
        <button
          type="button"
          className={`${styles.statusTab} ${
            statusFilter === 'Confirmed' ? styles.statusTabActive : ''
          }`}
          onClick={() => setStatusFilter('Confirmed')}
        >
          <strong>{statusCounts.Confirmed}</strong>
          <span>Đã xác nhận</span>
        </button>
        <button
          type="button"
          className={`${styles.statusTab} ${
            statusFilter === 'Arrived' ? styles.statusTabActive : ''
          }`}
          onClick={() => setStatusFilter('Arrived')}
        >
          <strong>{statusCounts.Arrived}</strong>
          <span>Khách đã đến</span>
        </button>
        <button
          type="button"
          className={`${styles.statusTab} ${
            statusFilter === 'CompletedGroup' ? styles.statusTabActive : ''
          }`}
          onClick={() => setStatusFilter('CompletedGroup')}
        >
          <strong>{statusCounts.Completed}</strong>
          <span>Đã hoàn thành / Tạo phiếu</span>
        </button>
      </div>

      {error && <ErrorMessage message={error} onRetry={() => void load()} />}

      <div className={styles.filterCard}>
        <div className={styles.searchWrap}>
          <span>🔎</span>
          <input
            type="text"
            placeholder="Tìm theo tên khách hàng hoặc số điện thoại..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
        <div className={styles.selectGroup}>
          <select
            className={styles.filterSelect}
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            aria-label="Lọc loại cuộc hẹn"
          >
            <option value="">Tất cả loại hẹn</option>
            <option value="Repair">Sửa chữa</option>
            <option value="Pickup">Nhận thiết bị</option>
            <option value="WarrantyRevisit">Bảo hành lại</option>
          </select>
          <select
            className={styles.filterSelect}
            value={customerFilter}
            onChange={(e) => setCustomerFilter(e.target.value as 'all' | 'account' | 'guest')}
            aria-label="Lọc khách hàng"
          >
            <option value="all">Tất cả khách hàng</option>
            <option value="account">Khách có tài khoản</option>
            <option value="guest">Khách vãng lai (Guest)</option>
          </select>
          {(searchQuery || statusFilter || typeFilter || customerFilter !== 'all') && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSearchQuery('');
                setStatusFilter('');
                setTypeFilter('');
                setCustomerFilter('all');
              }}
            >
              Xóa lọc
            </Button>
          )}
        </div>
      </div>

      <Table
        columns={columns}
        data={pagedAppointments}
        keyExtractor={(a) => a.id}
        isLoading={isLoading}
        emptyMessage="Không có lịch hẹn nào phù hợp với bộ lọc hiện tại."
        onRowClick={(a) => navigate(`/staff/appointments/${a.id}`)}
      />
      <Pagination
        page={page}
        pageSize={PAGE_SIZE}
        total={filteredAppointments.length}
        onPageChange={setPage}
      />

      {/* Modal nhập lý do từ chối */}
      <Modal
        isOpen={!!rejectingAppointment}
        onClose={() => setRejectingAppointment(null)}
        title="Từ chối lịch hẹn"
      >
        <p>Bạn có chắc chắn muốn từ chối lịch hẹn này? Vui lòng nhập lý do bên dưới.</p>

        <div className={styles.modalField}>
          <label className={styles.modalLabel}>
            Lý do từ chối <span className={styles.requiredMark}>*</span>
          </label>
          <textarea
            rows={3}
            className={`${styles.modalTextarea} ${reasonError ? styles.modalTextareaError : ''}`}
            placeholder="Nhập lý do từ chối (vd: Hết linh kiện, Kỹ thuật viên bận...)"
            value={rejectionReason}
            onChange={(e) => {
              setRejectionReason(e.target.value);
              if (reasonError) setReasonError(null);
            }}
          />
          {reasonError && <span className={styles.fieldError}>{reasonError}</span>}
        </div>

        <div className={styles.modalFooter}>
          <Button variant="secondary" onClick={() => setRejectingAppointment(null)}>
            Hủy
          </Button>
          <Button variant="danger" onClick={() => void handleConfirmReject()}>
            Xác nhận từ chối
          </Button>
        </div>
      </Modal>

      {/* Modal xác nhận hoàn thành lịch hẹn */}
      <Modal
        isOpen={!!completingAppointment}
        onClose={() => {
          if (isCompleting) return;
          setCompletingAppointment(null);
        }}
        title="Hoàn thành lịch hẹn"
      >
        <p>
          Bạn có chắc chắn muốn đánh dấu lịch hẹn này là <strong>Hoàn thành</strong>?
        </p>
        {completingAppointment && (
          <div className={styles.summaryBox}>
            <div>
              Khách hàng: <strong>{completingAppointment.fullName}</strong>
            </div>
            <div>Số điện thoại: {completingAppointment.phone}</div>
            <div>
              Ngày hẹn:{' '}
              {new Date(completingAppointment.appointmentDate).toLocaleDateString('vi-VN')} (
              {completingAppointment.slotStart} - {completingAppointment.slotEnd})
            </div>
          </div>
        )}
        <p className={styles.modalHint}>
          Sau khi hoàn thành, lịch hẹn sẽ được lưu vào lịch sử phục vụ.
        </p>

        <div className={styles.modalFooter}>
          <Button
            variant="secondary"
            onClick={() => setCompletingAppointment(null)}
            disabled={isCompleting}
          >
            Hủy
          </Button>
          <Button
            variant="success"
            onClick={() => void handleConfirmComplete()}
            disabled={isCompleting}
          >
            {isCompleting ? 'Đang xử lý...' : 'Xác nhận hoàn thành'}
          </Button>
        </div>
      </Modal>
    </div>
  );
}
