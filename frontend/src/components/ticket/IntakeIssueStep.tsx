import { type FormEvent, useMemo, useState } from 'react';
import { Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import { DEVICE_TYPE_LABELS, type Device, type DeviceType } from '../../types/device.types';
import type { Customer } from '../../types/customer.types';
import type { Ticket } from '../../types/ticket.types';
import { useToast } from '../../hooks/useToast';
import styles from './IntakeStep.module.css';

interface IntakeIssueStepProps {
  customer: Customer;
  device: Device;
  onBack: () => void;
  onCreated: (ticket: Ticket) => void;
}

const SYMPTOM_PRESETS_BY_DEVICE: Record<DeviceType, string[]> = {
  Phone: [
    'Màn hình vỡ / nứt kính / liệt cảm ứng',
    'Màn hình sọc / loang mực / không hiển thị',
    'Pin tụt nhanh / chai pin / phù pin',
    'Không nhận sạc / sạc chập chờn / lỏng chân sạc',
    'Máy không lên nguồn / tự tắt nguồn đột ngột',
    'Lỗi loa / mic / nghe gọi nhỏ hoặc rè',
    'Camera mờ / rung / không lấy nét / mất Face ID',
    'Máy bị vô nước / ẩm bo mạch',
    'Mất sóng / Wi-Fi yếu / lỗi Bluetooth',
  ],
  Laptop: [
    'Máy không lên nguồn / kích nguồn không chạy',
    'Màn hình tối / sọc / chớp nháy / hở sáng',
    'Bàn phím liệt nút / kẹt phím / loạn phím',
    'Pin chai / báo thay pin / sạc không vào điện',
    'Quạt kêu to / máy quá nhiệt / tự sập nguồn',
    'Hư bản lề / bung khung vỏ màn hình',
    'Lỗi ổ cứng / không boot vào hệ điều hành',
    'Trackpad không nhạy / cổng USB-C hỏng',
    'Vô nước bàn phím / chập mainboard',
  ],
  Tablet: [
    'Vỡ mặt kính / liệt hoặc loạn cảm ứng',
    'Pin hao nhanh / sạc chậm / không vào pin',
    'Không lên nguồn / treo logo khi khởi động',
    'Hỏng cổng sạc Type-C / Lightning',
    'Cong khung vỏ / hở màn hình',
    'Loa rè / mất âm thanh / lỗi kết nối Wi-Fi',
    'Máy vô nước / ẩm linh kiện bên trong',
  ],
  Smartwatch: [
    'Nứt mặt kính / lỗi cảm ứng màn hình',
    'Pin tụt nhanh / phồng pin đẩy bung màn',
    'Không nhận sạc / sạc nóng bất thường',
    'Mất nguồn / không ghép đôi được điện thoại',
    'Hỏng nút xoay Digital Crown / nút nguồn',
    'Lỗi cảm biến nhịp tim / vào nước',
  ],
  Electronics: [
    'Thiết bị không lên nguồn / mất tín hiệu',
    'Pin chai / sạc không vào / tự ngắt nguồn',
    'Âm thanh rè / mất một bên tai nghe hoặc loa',
    'Lỗi kết nối Bluetooth / cổng cắm tín hiệu',
    'Trôi cần điều khiển / kẹt phím bấm',
    'Chập mạch do ẩm nước / quá áp',
  ],
};

const CONDITION_PRESETS: string[] = [
  'Máy nguyên vẹn, ngoại quan đẹp',
  'Trầy xước nhẹ vỏ / viền',
  'Cấn móp góc / khung sườn',
  'Nứt mặt kính trước / kính lưng',
  'Bong keo / hở viền màn hình',
  'Thiếu khay SIM / phụ kiện đi kèm',
  'Tem bảo hành rách / máy đã qua sửa chữa',
  'Không kiểm tra được chức năng do mất nguồn',
];

const RISK_WARNING_PRESETS: string[] = [
  'Máy vào nước có nguy cơ phát sinh lỗi ẩn trên mainboard',
  'Tháo máy kiểm tra có thể làm giảm khả năng kháng nước ban đầu',
  'Màn hình nứt sẵn có rủi ro sọc/tối thêm khi bóc tách',
  'Đã tư vấn khách hàng tự sao lưu dữ liệu cá nhân',
  'Máy mất nguồn chưa thể kiểm tra toàn bộ chức năng khác',
];

const DEPOSIT_PRESETS: { label: string; value: number }[] = [
  { label: '0 đ - Không thu', value: 0 },
  { label: '50.000 đ', value: 50000 },
  { label: '100.000 đ', value: 100000 },
  { label: '200.000 đ', value: 200000 },
  { label: '300.000 đ', value: 300000 },
  { label: '500.000 đ', value: 500000 },
];

function toggleItem(item: string, list: string[], setList: (next: string[]) => void) {
  setList(list.includes(item) ? list.filter((i) => i !== item) : [...list, item]);
}

function combineChecklistAndNote(selected: string[], note: string): string {
  const parts = [...selected];
  const trimmedNote = note.trim();
  if (trimmedNote) parts.push(trimmedNote);
  return parts.join('; ');
}

export default function IntakeIssueStep({
  customer,
  device,
  onBack,
  onCreated,
}: IntakeIssueStepProps) {
  const [selectedIssues, setSelectedIssues] = useState<string[]>([]);
  const [issueExtra, setIssueExtra] = useState('');

  const [selectedConditions, setSelectedConditions] = useState<string[]>([]);
  const [conditionExtra, setConditionExtra] = useState('');

  const [selectedRisks, setSelectedRisks] = useState<string[]>([]);
  const [riskExtra, setRiskExtra] = useState('');

  const [diagnosticDeposit, setDiagnosticDeposit] = useState('0');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess, showError } = useToast();

  const issueOptions = useMemo(
    () => SYMPTOM_PRESETS_BY_DEVICE[device.deviceType] ?? SYMPTOM_PRESETS_BY_DEVICE.Electronics,
    [device.deviceType]
  );

  const combinedIssue = useMemo(
    () => combineChecklistAndNote(selectedIssues, issueExtra),
    [selectedIssues, issueExtra]
  );

  const combinedCondition = useMemo(
    () => combineChecklistAndNote(selectedConditions, conditionExtra),
    [selectedConditions, conditionExtra]
  );

  const combinedRisk = useMemo(
    () => combineChecklistAndNote(selectedRisks, riskExtra),
    [selectedRisks, riskExtra]
  );

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setErrors([]);

    if (!combinedIssue) {
      setErrorMessage('Vui lòng chọn ít nhất một triệu chứng hoặc nhập mô tả lỗi khách hàng khai báo.');
      return;
    }

    if (combinedIssue.length > 1000) {
      setErrorMessage('Mô tả lỗi tổng hợp không được vượt quá 1000 ký tự.');
      return;
    }

    const depositNumber = diagnosticDeposit.trim() ? Number(diagnosticDeposit) : 0;
    if (Number.isNaN(depositNumber) || depositNumber < 0) {
      setErrorMessage('Tiền cọc chẩn đoán phải là số lớn hơn hoặc bằng 0.');
      return;
    }

    setIsSaving(true);
    try {
      const ticket = await ticketService.create({
        customerId: customer.id,
        deviceId: device.id,
        issueDescription: combinedIssue,
        conditionNotes: combinedCondition || undefined,
        riskWarning: combinedRisk || undefined,
        diagnosticDeposit: depositNumber,
      });
      showSuccess(`Đã tạo phiếu ${ticket.ticketCode} thành công.`);
      onCreated(ticket);
    } catch (err) {
      showError('Không thể tạo phiếu. Vui lòng thử lại.');
      const apiError = extractApiError(err);
      setErrorMessage(apiError.message);
      setErrors(apiError.errors);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <div className={styles.selectedCard}>
        <span>
          <strong>{customer.fullName}</strong> — {device.brand} {device.model}
        </span>
        <span className={styles.badge}>
          {DEVICE_TYPE_LABELS[device.deviceType] ?? device.deviceType}
        </span>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} errors={errors} />}

      {/* 1. Checklist mô tả lỗi */}
      <section className={styles.checklistSection}>
        <div className={styles.sectionTitleRow}>
          <label className={styles.label}>
            1. Mô tả lỗi khách hàng khai báo *
          </label>
          <span className={styles.selectedCount}>
            Đã chọn {selectedIssues.length} mục
          </span>
        </div>
        <p className={styles.hint}>
          Chọn nhanh các biểu hiện lỗi phổ biến theo dòng thiết bị hoặc nhập thêm chi tiết bên dưới.
        </p>

        <div className={styles.checklistGrid}>
          {issueOptions.map((item) => {
            const checked = selectedIssues.includes(item);
            return (
              <label
                key={item}
                className={`${styles.checkCard} ${checked ? styles.checkCardSelected : ''}`}
              >
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={() => toggleItem(item, selectedIssues, setSelectedIssues)}
                />
                <span>{item}</span>
              </label>
            );
          })}
        </div>

        <textarea
          className={styles.textarea}
          rows={2}
          value={issueExtra}
          onChange={(e) => setIssueExtra(e.target.value)}
          placeholder="Mô tả lỗi khác / chi tiết thêm (VD: máy nóng khi gọi video, rớt nhẹ 2 ngày trước...)"
        />

        {combinedIssue && (
          <div className={styles.previewBox}>
            <span className={styles.previewLabel}>Nội dung ghi nhận:</span>
            <p className={styles.previewText}>{combinedIssue}</p>
          </div>
        )}
      </section>

      {/* 2. Checklist tình trạng ngoại quan ban đầu */}
      <section className={styles.checklistSection}>
        <div className={styles.sectionTitleRow}>
          <label className={styles.label}>2. Tình trạng ngoại quan ban đầu</label>
          <span className={styles.selectedCount}>
            Đã chọn {selectedConditions.length} mục
          </span>
        </div>

        <div className={styles.checklistGrid}>
          {CONDITION_PRESETS.map((item) => {
            const checked = selectedConditions.includes(item);
            return (
              <label
                key={item}
                className={`${styles.checkCard} ${checked ? styles.checkCardSelected : ''}`}
              >
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={() => toggleItem(item, selectedConditions, setSelectedConditions)}
                />
                <span>{item}</span>
              </label>
            );
          })}
        </div>

        <textarea
          className={styles.textarea}
          rows={2}
          value={conditionExtra}
          onChange={(e) => setConditionExtra(e.target.value)}
          placeholder="Ghi chú ngoại quan bổ sung (VD: trầy nhẹ cạnh trên bên phải, thiếu ốc đáy...)"
        />
      </section>

      {/* 3. Checklist cảnh báo rủi ro */}
      <section className={styles.checklistSection}>
        <div className={styles.sectionTitleRow}>
          <label className={styles.label}>3. Cảnh báo rủi ro cho khách hàng (nếu có)</label>
          <span className={styles.selectedCount}>
            Đã chọn {selectedRisks.length} mục
          </span>
        </div>

        <div className={styles.checklistGrid}>
          {RISK_WARNING_PRESETS.map((item) => {
            const checked = selectedRisks.includes(item);
            return (
              <label
                key={item}
                className={`${styles.checkCard} ${checked ? styles.checkCardSelectedWarning : ''}`}
              >
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={() => toggleItem(item, selectedRisks, setSelectedRisks)}
                />
                <span>{item}</span>
              </label>
            );
          })}
        </div>

        <textarea
          className={styles.textarea}
          rows={2}
          value={riskExtra}
          onChange={(e) => setRiskExtra(e.target.value)}
          placeholder="Ghi chú cảnh báo rủi ro bổ sung (VD: máy mất mật khẩu màn hình, cần reset nếu chạy lại phần mềm...)"
        />
      </section>

      {/* 4. Tiền cọc chẩn đoán */}
      <section className={styles.checklistSection}>
        <label className={styles.label}>4. Tiền cọc chẩn đoán (nếu thu)</label>
        <div className={styles.depositChips}>
          {DEPOSIT_PRESETS.map((preset) => {
            const active = Number(diagnosticDeposit || 0) === preset.value;
            return (
              <button
                key={preset.value}
                type="button"
                className={`${styles.depositChip} ${active ? styles.depositChipActive : ''}`}
                onClick={() => setDiagnosticDeposit(String(preset.value))}
              >
                {preset.label}
              </button>
            );
          })}
        </div>
        <div className={styles.depositInputRow}>
          <input
            type="number"
            min={0}
            step={10000}
            className={styles.numberInput}
            value={diagnosticDeposit}
            onChange={(e) => setDiagnosticDeposit(e.target.value)}
            placeholder="0"
          />
          <span className={styles.currencyUnit}>VNĐ</span>
        </div>
      </section>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onBack} disabled={isSaving}>
          ← Quay lại
        </Button>
        <Button type="submit" isLoading={isSaving}>
          Tạo phiếu sửa chữa
        </Button>
      </div>
    </form>
  );
}
