import { useEffect, useState } from 'react';
import { Modal, Button, ErrorMessage, Badge } from '../common';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import { appointmentService, type AvailableSlot } from '../../services/appointmentService';
import type { MyWarrantyItem } from '../../types/warranty.types';
import styles from './WarrantyClaimModal.module.css';

interface Props {
  isOpen: boolean;
  ticketId: string;
  warrantyItem?: MyWarrantyItem | null;
  onClose: () => void;
  onDone: () => void;
}

const WARRANTY_SYMPTOM_CHIPS: string[] = [
  'Tái phát lỗi cũ sau sửa chữa',
  'Linh kiện thay thế hoạt động không ổn định',
  'Pin hao nhanh / máy nóng bất thường',
  'Cảm ứng / hiển thị màn hình chập chờn',
  'Loa / mic / kết nối tín hiệu không ổn định',
];

function getTodayDateString(): string {
  const now = new Date();
  const offsetMs = now.getTimezoneOffset() * 60000;
  return new Date(now.getTime() - offsetMs).toISOString().slice(0, 10);
}

export default function WarrantyClaimModal({
  isOpen,
  ticketId,
  warrantyItem,
  onClose,
  onDone,
}: Props) {
  const { showSuccess } = useToast();
  const today = getTodayDateString();
  const [issueReported, setIssueReported] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [appointmentDate, setAppointmentDate] = useState(today);
  const [timeSlotId, setTimeSlotId] = useState('');
  const [slots, setSlots] = useState<AvailableSlot[]>([]);

  useEffect(() => {
    if (!isOpen) return;
    setErrorMessage(null);
    setIssueReported('');
    setTimeSlotId('');
    setAppointmentDate(getTodayDateString());
  }, [isOpen, ticketId]);

  useEffect(() => {
    if (!isOpen || !appointmentDate) return;
    let cancelled = false;
    setIsLoadingSlots(true);
    appointmentService
      .getAvailableSlots(appointmentDate)
      .then((data) => {
        if (!cancelled) setSlots(data);
      })
      .catch((err) => {
        if (!cancelled) {
          setSlots([]);
          setErrorMessage(extractApiError(err).message);
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoadingSlots(false);
      });
    return () => {
      cancelled = true;
    };
  }, [isOpen, appointmentDate]);

  const toggleSymptomChip = (chip: string) => {
    const parts = issueReported
      .split(';')
      .map((s) => s.trim())
      .filter(Boolean);
    if (parts.includes(chip)) {
      setIssueReported(parts.filter((p) => p !== chip).join('; '));
    } else {
      setIssueReported([...parts, chip].join('; '));
    }
  };

  const handleSubmit = async () => {
    if (!issueReported.trim()) {
      setErrorMessage('Vui lòng mô tả vấn đề gặp phải.');
      return;
    }
    if (!timeSlotId) {
      setErrorMessage('Vui lòng chọn ngày và khung giờ nhận thiết bị.');
      return;
    }
    setErrorMessage(null);
    setIsSaving(true);
    try {
      await warrantyService.createClaim(ticketId, issueReported.trim(), appointmentDate, timeSlotId);
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
      title="Tạo lịch hẹn yêu cầu bảo hành"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={() => void handleSubmit()} isLoading={isSaving}>
            Gửi yêu cầu bảo hành
          </Button>
        </>
      }
    >
      <div className={styles.container}>
        {warrantyItem && (
          <div className={styles.summaryCard}>
            <div className={styles.summaryLeft}>
              <div className={styles.shieldIcon}>🛡️</div>
              <div className={styles.summaryInfo}>
                <strong className={styles.deviceTitle}>{warrantyItem.deviceLabel}</strong>
                <div className={styles.metaBadges}>
                  <span className={styles.codeChip}>Mã BH: {warrantyItem.warrantyCode}</span>
                  <span className={styles.codeChip}>Phiếu gốc: {warrantyItem.ticketCode}</span>
                </div>
                <span className={styles.expiryText}>
                  Hiệu lực đến:{' '}
                  {new Date(warrantyItem.endDate).toLocaleDateString('vi-VN')}
                </span>
              </div>
            </div>
            <Badge variant="success">Còn bảo hành</Badge>
          </div>
        )}

        {errorMessage && <ErrorMessage message={errorMessage} />}

        <div className={styles.field}>
          <label className={styles.label}>Triệu chứng bảo hành thường gặp</label>
          <div className={styles.chipGroup}>
            {WARRANTY_SYMPTOM_CHIPS.map((chip) => {
              const isSelected = issueReported
                .split(';')
                .map((s) => s.trim())
                .includes(chip);
              return (
                <button
                  key={chip}
                  type="button"
                  className={`${styles.symptomChip} ${
                    isSelected ? styles.symptomChipActive : ''
                  }`}
                  onClick={() => toggleSymptomChip(chip)}
                >
                  {isSelected ? '✓ ' : '+ '}
                  {chip}
                </button>
              );
            })}
          </div>
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="warranty-issue-input">
            Mô tả vấn đề gặp phải *
          </label>
          <textarea
            id="warranty-issue-input"
            rows={3}
            className={styles.textarea}
            value={issueReported}
            onChange={(e) => setIssueReported(e.target.value)}
            placeholder="VD: Máy lại bị tình trạng pin tụt nhanh như lần trước..."
          />
        </div>

        <div className={styles.scheduleSection}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="warranty-date-input">
              Ngày mang thiết bị đến cửa hàng *
            </label>
            <input
              id="warranty-date-input"
              type="date"
              className={styles.dateInput}
              min={today}
              value={appointmentDate}
              onChange={(e) => {
                setAppointmentDate(e.target.value);
                setTimeSlotId('');
              }}
            />
          </div>

          <div className={styles.field}>
            <label className={styles.label}>Chọn khung giờ tiếp nhận *</label>
            {isLoadingSlots ? (
              <p className={styles.emptySlots}>Đang tải danh sách khung giờ khả dụng...</p>
            ) : slots.length === 0 ? (
              <p className={styles.emptySlots}>
                Chưa có khung giờ nào khả dụng trong ngày đã chọn.
              </p>
            ) : (
              <div className={styles.slotGrid}>
                {slots.map((slot) => {
                  const isSelected = timeSlotId === slot.timeSlotId;
                  return (
                    <button
                      key={slot.timeSlotId}
                      type="button"
                      disabled={slot.isFull}
                      className={`${styles.slotCard} ${
                        isSelected ? styles.slotCardSelected : ''
                      }`}
                      onClick={() => setTimeSlotId(slot.timeSlotId)}
                    >
                      <span className={styles.slotTime}>
                        {slot.slotStart.slice(0, 5)} – {slot.slotEnd.slice(0, 5)}
                      </span>
                      <span className={styles.slotCapacity}>
                        {slot.isFull
                          ? 'Đã đầy chỗ'
                          : `Còn ${slot.available}/${slot.maxCapacity} chỗ`}
                      </span>
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </Modal>
  );
}
