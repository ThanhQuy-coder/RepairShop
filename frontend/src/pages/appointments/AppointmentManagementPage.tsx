import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import {
  Badge,
  Button,
  ErrorMessage,
  Modal,
  Table,
  type TableColumn,
} from '../../components/common';
import { extractApiError } from '../../utils/apiError';
import type { AppointmentIntakeData } from '../../store/ticketIntakeStore';

const statusLabels: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Rejected: 'Đã từ chối',
  Cancelled: 'Đã hủy',
  Arrived: 'Đã chuyển ticket',
  Complete: 'Hoàn thành',
  NoShow: 'Không đến',
};

export default function AppointmentManagementPage() {
  const navigate = useNavigate();
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
          'Phone' | 'Laptop' | 'Electronics' | 'Tablet' | 'Smartwatch',
        brand: detail.brand,
        model: detail.model,
        serialNumber: detail.serialNumber,
      };

      navigate('/tickets/create', {
        state: { appointment },
      });
    } catch (error) {
      console.error('Không thể lấy chi tiết lịch hẹn:', error);
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

  const transition = async (
    id: string,
    rejectionReason: string,
    action: 'confirm' | 'reject' | 'noShow' | 'markCompleted'
  ) => {
    try {
      await appointmentService[action](id, rejectionReason);
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

  const columns: TableColumn<AppointmentResponse>[] = [
    { key: 'date', header: 'Ngày hẹn', render: (a) => a.appointmentDate },
    {
      key: 'name',
      header: 'Khách hàng',
      render: (a) => (
        <>
          <strong>{a.fullName}</strong>
          <br />
          <small>{a.phone}</small>
        </>
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
              : a.status === 'Rejected'
                ? 'danger'
                : a.status === 'NoShow'
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
      header: 'Thao tác',
      render: (a) => (
        <div
          style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}
          onClick={(event) => event.stopPropagation()}
        >
          {a.status === 'Pending' && (
            <>
              <Button size="sm" onClick={() => void transition(a.id, 'đã xác nhận', 'confirm')}>
                Xác nhận
              </Button>
              <Button size="sm" variant="danger" onClick={() => handleOpenRejectModal(a)}>
                Từ chối
              </Button>
            </>
          )}
          {a.status === 'Confirmed' && (
            <>
              <Button size="sm" onClick={() => handleCreateTicket(a)}>
                Chuyển thành ticket
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={() => transition(a.id, 'khách không đến', 'noShow')}
              >
                Không đến
              </Button>
            </>
          )}
          {a.status === 'Arrived' && (
            <>
              <Button size="sm" onClick={() => handleCreateTicket(a)}>
                Chuyển thành ticket
              </Button>
              <Button size="sm" variant="danger" onClick={() => handleOpenCompleteModal(a)}>
                Hoàn thành
              </Button>
            </>
          )}{' '}
        </div>
      ),
    },
  ];

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 24 }}>
        <span style={{ color: '#64748b', fontSize: 12, letterSpacing: 1 }}>TIẾP NHẬN DỊCH VỤ</span>
        <h1>Lịch hẹn khách hàng</h1>
        <p style={{ color: '#64748b' }}>
          Xác nhận, từ chối và chuyển lịch hẹn đã xác nhận thành phiếu sửa chữa.
        </p>
      </div>
      {error && <ErrorMessage message={error} onRetry={() => void load()} />}
      <Table
        columns={columns}
        data={appointments}
        keyExtractor={(a) => a.id}
        isLoading={isLoading}
        emptyMessage="Chưa có lịch hẹn."
        onRowClick={(a) => navigate(`/staff/appointments/${a.id}`)}
      />

      {/* Modal nhập lý do từ chối */}
      <Modal
        isOpen={!!rejectingAppointment}
        onClose={() => setRejectingAppointment(null)}
        title="Từ chối lịch hẹn"
      >
        <p>Bạn có chắc chắn muốn từ chối lịch hẹn này? Vui lòng nhập lý do bên dưới.</p>

        <div style={{ marginBottom: 12 }}>
          <label style={{ display: 'block', marginBottom: 6, fontWeight: 500 }}>
            Lý do từ chối <span style={{ color: '#ef4444' }}>*</span>
          </label>
          <textarea
            rows={3}
            style={{
              width: '100%',
              padding: '8px 12px',
              borderRadius: 6,
              border: `1px solid ${reasonError ? '#ef4444' : '#cbd5e1'}`,
              outline: 'none',
              resize: 'vertical',
            }}
            placeholder="Nhập lý do từ chối (vd: Hết linh kiện, Kỹ thuật viên bận...)"
            value={rejectionReason}
            onChange={(e) => {
              setRejectionReason(e.target.value);
              if (reasonError) setReasonError(null); // Clear error khi gõ
            }}
          />
          {reasonError && (
            <span style={{ color: '#ef4444', fontSize: 13, marginTop: 4, display: 'block' }}>
              {reasonError}
            </span>
          )}
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 16 }}>
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
          <div
            style={{
              background: '#f8fafc',
              border: '1px solid #e2e8f0',
              borderRadius: 6,
              padding: '10px 12px',
              marginTop: 8,
              fontSize: 14,
              color: '#334155',
            }}
          >
            <div>
              Khách hàng: <strong>{completingAppointment.fullName}</strong>
            </div>
            <div>Số điện thoại: {completingAppointment.phone}</div>
            <div>Ngày hẹn: {completingAppointment.appointmentDate}</div>
          </div>
        )}
        <p style={{ color: '#64748b', fontSize: 13, marginTop: 12 }}>
          Sau khi hoàn thành, lịch hẹn sẽ không thể thay đổi trạng thái.
        </p>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 16 }}>
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
