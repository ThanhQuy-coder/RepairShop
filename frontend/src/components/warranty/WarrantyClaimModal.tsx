import { useEffect, useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import { appointmentService, type AvailableSlot } from '../../services/appointmentService';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function WarrantyClaimModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const { showSuccess } = useToast();
  const [issueReported, setIssueReported] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [appointmentDate, setAppointmentDate] = useState(new Date().toISOString().slice(0, 10));
  const [timeSlotId, setTimeSlotId] = useState('');
  const [slots, setSlots] = useState<AvailableSlot[]>([]);

  useEffect(() => {
    void appointmentService.getAvailableSlots(appointmentDate).then(setSlots);
  }, []);

  const handleSubmit = async () => {
    if (!issueReported.trim()) {
      setErrorMessage('Vui lòng mô tả vấn đề gặp phải.');
      return;
    }
    setErrorMessage(null);
    setIsSaving(true);
    if (!timeSlotId) {
      setErrorMessage('Vui lòng chọn ngày và khung giờ nhận thiết bị.');
      return;
    }
    try {
      await warrantyService.createClaim(ticketId, issueReported, appointmentDate, timeSlotId);
      showSuccess('Đã tạo yêu cầu bảo hành. Vui lòng mang thiết bị đến cửa hàng.');
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
      title="Yêu cầu bảo hành"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Gửi yêu cầu
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <label style={{ fontSize: 13, fontWeight: 500, display: 'block', marginBottom: 4 }}>
        Mô tả vấn đề gặp phải *
      </label>
      <textarea
        rows={3}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        value={issueReported}
        onChange={(e) => setIssueReported(e.target.value)}
        placeholder="VD: Máy lại bị tình trạng pin tụt nhanh như lần trước..."
      />
      <label style={{ display: 'block', marginTop: 12 }}>Ngày đến bảo hành</label>
      <input type="date" min={new Date().toISOString().slice(0, 10)} value={appointmentDate}
        onChange={(e) => {
          setAppointmentDate(e.target.value);
          setTimeSlotId('');
          void appointmentService.getAvailableSlots(e.target.value).then(setSlots);
        }} />
      <label style={{ display: 'block', marginTop: 12 }}>Khung giờ</label>
      <select value={timeSlotId} onChange={(e) => setTimeSlotId(e.target.value)} style={{ width: '100%', padding: 8 }}>
        <option value="">Chọn khung giờ</option>
        {slots.filter((slot) => !slot.isFull).map((slot) => (
          <option key={slot.timeSlotId} value={slot.timeSlotId}>{slot.slotStart} - {slot.slotEnd}</option>
        ))}
      </select>
    </Modal>
  );
}
