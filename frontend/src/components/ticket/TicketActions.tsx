import { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import type { TicketDetail } from '../../types/ticket.types';
import type { Quote } from '../../types/quote.types';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import { Button, ErrorMessage } from '../common';
import AssignTechnicianModal from './AssignTechnicianModal';
import SubmitDiagnosisModal from './SubmitDiagnosisModal';
import CompleteRepairModal from './CompleteRepairModal';
import QaPassModal from './QaPassModal';
import QaFailModal from './QaFailModal';
import DeliveryModal from './DeliveryModal';
import WarrantyModal from './WarrantyModal';

interface TicketActionsProps {
  ticket: TicketDetail;
  quotes: Quote[];
  onUpdated: () => void;
}

/**
 * Component DUY NHẤT quyết định "nút nào hiện ra" — dựa hoàn toàn vào 2 thứ:
 * (1) ticket.status hiện tại, (2) role người đang đăng nhập.
 * Component KHÔNG tự validate xem hành động có hợp lệ hay không — nó chỉ ẩn/hiện nút cho gọn UX.
 * Mọi validate thật (state machine, ownership, business rule) đều do Backend quyết định
 * (Task 4.2/4.6/4.16, Tuần 4) — nếu Frontend lỡ hiện sai nút, Backend vẫn trả lỗi rõ ràng,
 * KHÔNG BAO GIỜ tự Frontend coi hành động là hợp lệ.
 */
export default function TicketActions({ ticket, quotes, onUpdated }: TicketActionsProps) {
  const { role } = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [openModal, setOpenModal] = useState<string | null>(null);

  const isStaffOnTicket = role === 'Receptionist' || role === 'Admin';
  const isTechnicianOnTicket = role === 'Technician';

  const run = async (actionKey: string, fn: () => Promise<unknown>) => {
    setErrorMessage(null);
    setLoadingAction(actionKey);
    try {
      await fn();
      onUpdated();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setLoadingAction(null);
    }
  };

  const actions: {
    key: string;
    label: string;
    onClick: () => void;
    variant?: 'primary' | 'danger';
  }[] = [];

  if (isStaffOnTicket && ticket.status === 'CHECKED_IN') {
    actions.push({
      key: 'assign',
      label: 'Phân công kỹ thuật viên',
      onClick: () => setOpenModal('assign'),
    });
  }

  if (isTechnicianOnTicket && ticket.status === 'ASSIGNED') {
    actions.push({
      key: 'startDiagnosis',
      label: 'Bắt đầu chẩn đoán',
      onClick: () => run('startDiagnosis', () => ticketService.startDiagnosis(ticket.id)),
    });
  }

  if (isTechnicianOnTicket && ticket.status === 'DIAGNOSING') {
    actions.push({
      key: 'submitDiagnosis',
      label: 'Ghi kết quả chẩn đoán',
      onClick: () => setOpenModal('diagnosis'),
    });
  }

  // const approvedQuoteExists = quotes.some((q) => q.status === 'Approved');
  if (
    isTechnicianOnTicket &&
    ['WAITING_PARTS', 'IN_REPAIR'].includes(ticket.status) === false &&
    ticket.status === 'IN_REPAIR'
  ) {
    // (giữ để rõ ràng — nhánh IN_REPAIR xử lý bên dưới, tránh trùng điều kiện)
  }

  if (isTechnicianOnTicket && ticket.status === 'IN_REPAIR') {
    actions.push({
      key: 'completeRepair',
      label: 'Hoàn tất sửa chữa',
      onClick: () => setOpenModal('completeRepair'),
    });
  }

  if (isTechnicianOnTicket && ticket.status === 'QA_TESTING') {
    actions.push({ key: 'qaPass', label: 'QA — Đạt', onClick: () => setOpenModal('qaPass') });
    actions.push({
      key: 'qaFail',
      label: 'QA — Không đạt',
      variant: 'danger',
      onClick: () => setOpenModal('qaFail'),
    });
  }

  if (isStaffOnTicket && ticket.status === 'READY_FOR_PICKUP') {
    actions.push({
      key: 'deliver',
      label: 'Xuất hóa đơn & Bàn giao',
      onClick: () => setOpenModal('deliver'),
    });
  }

  if (isStaffOnTicket && ticket.status === 'DELIVERED') {
    actions.push({
      key: 'warranty',
      label: 'Tạo thông tin bảo hành',
      onClick: () => setOpenModal('warranty'),
    });
  }

  if (actions.length === 0) {
    return (
      <p style={{ color: 'var(--color-text-muted)', fontSize: 14 }}>
        Không có hành động nào khả dụng ở trạng thái hiện tại cho vai trò của bạn.
      </p>
    );
  }

  return (
    <div>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {actions.map((a) => (
          <Button
            key={a.key}
            variant={a.variant ?? 'primary'}
            isLoading={loadingAction === a.key}
            onClick={a.onClick}
          >
            {a.label}
          </Button>
        ))}
      </div>

      <AssignTechnicianModal
        isOpen={openModal === 'assign'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <SubmitDiagnosisModal
        isOpen={openModal === 'diagnosis'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <CompleteRepairModal
        isOpen={openModal === 'completeRepair'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <QaPassModal
        isOpen={openModal === 'qaPass'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <QaFailModal
        isOpen={openModal === 'qaFail'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <DeliveryModal
        isOpen={openModal === 'deliver'}
        ticketId={ticket.id}
        approvedQuote={quotes.find((q) => q.status === 'Approved')}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />

      <WarrantyModal
        isOpen={openModal === 'warranty'}
        ticketId={ticket.id}
        onClose={() => setOpenModal(null)}
        onDone={() => {
          setOpenModal(null);
          onUpdated();
        }}
      />
    </div>
  );
}
