import { useEffect, useState } from 'react';
import { Modal, Button, Select, ErrorMessage } from '../common';
import { userService } from '../../services/userService';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { UserListItem } from '../../types/user.types';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function AssignTechnicianModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [technicians, setTechnicians] = useState<UserListItem[]>([]);
  const [technicianId, setTechnicianId] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen)
      userService
        .list({ role: 'Technician', isActive: true, pageSize: 100 })
        .then((r) => setTechnicians(r.items));
  }, [isOpen]);

  const handleSubmit = async () => {
    if (!technicianId) {
      setErrorMessage('Vui lòng chọn kỹ thuật viên.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.assignTechnician(ticketId, technicianId);
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Phân công kỹ thuật viên"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Xác nhận
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <Select
        label="Kỹ thuật viên"
        placeholder="-- Chọn --"
        value={technicianId}
        options={technicians.map((t) => ({ value: t.id, label: t.fullName }))}
        onChange={(e) => setTechnicianId(e.target.value)}
      />
    </Modal>
  );
}
