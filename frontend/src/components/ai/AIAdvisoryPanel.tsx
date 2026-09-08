import { type FormEvent, useState } from 'react';
import { aiAdvisoryService } from '../../services/aiAdvisoryService';
import { extractApiError } from '../../utils/apiError';
import type { AIAdviceResponse } from '../../types/aiAdvisory.types';
import type { DeviceType } from '../../types/device.types';
import { Button, ErrorMessage } from '../common';
import styles from './AIAdvisoryPanel.module.css';

interface AIAdvisoryPanelProps {
  deviceType: DeviceType;
  brand: string;
  model: string;
  initialIssueDescription?: string;
}

const DEVICE_TYPE_LABEL: Record<string, string> = {
  Phone: 'Điện thoại',
  Laptop: 'Laptop',
  Electronics: 'Thiết bị điện tử khác',
};

/**
 * Component dùng chung cho Receptionist (nhúng vào Intake Wizard / Ticket Detail) và Customer
 * (nhúng vào form yêu cầu sửa chữa online) — khớp Authorization contract Task 4 mục 13.1 Tuần 2:
 * chỉ Customer và Receptionist mới thấy nút "Tư vấn AI" (Controller đã enforce ở Backend,
 * Frontend chỉ cần đặt component này vào đúng 2 nơi có role phù hợp).
 */
export default function AIAdvisoryPanel({
  deviceType,
  brand,
  model,
  initialIssueDescription = '',
}: AIAdvisoryPanelProps) {
  const [issueDescription, setIssueDescription] = useState(initialIssueDescription);
  const [advice, setAdvice] = useState<AIAdviceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!issueDescription.trim()) {
      setErrorMessage('Vui lòng mô tả tình trạng thiết bị trước khi tư vấn.');
      return;
    }

    setErrorMessage(null);
    setAdvice(null);
    setIsLoading(true);

    try {
      const result = await aiAdvisoryService.getAdvice({
        deviceType,
        brand,
        model,
        issueDescription: issueDescription.trim(),
      });
      setAdvice(result);
    } catch (err) {
      // Lỗi HTTP thật (400/401/403/500 từ chính Backend, KHÔNG phải AI down — AI down vẫn trả 200
      // theo đúng Task 6.18/6.19, nên nhánh catch này chỉ bắt lỗi tầng Backend/network thực sự).
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.panel}>
      <div className={styles.deviceInfo}>
        <h4>Thông tin thiết bị</h4>
        <p>
          Loại: <strong>{DEVICE_TYPE_LABEL[deviceType] ?? deviceType}</strong>
        </p>
        <p>
          Hãng: <strong>{brand}</strong>
        </p>
        <p>
          Model: <strong>{model}</strong>
        </p>
      </div>

      <form onSubmit={handleSubmit} className={styles.form}>
        <label className={styles.label}>Mô tả tình trạng</label>
        <textarea
          className={styles.textarea}
          rows={3}
          maxLength={500}
          value={issueDescription}
          onChange={(e) => setIssueDescription(e.target.value)}
          placeholder="VD: Pin tụt nhanh, máy nóng khi sạc..."
        />
        <span className={styles.charCount}>{issueDescription.length}/500</span>

        {errorMessage && <ErrorMessage message={errorMessage} />}

        <Button type="submit" isLoading={isLoading}>
          🤖 Tư vấn AI
        </Button>
      </form>

      {isLoading && (
        <div className={styles.loadingBox}>
          <div className={styles.spinner} />
          <span>AI đang phân tích...</span>
        </div>
      )}

      {!isLoading && advice && <AIAdvisoryResult advice={advice} />}
    </div>
  );
}

function AIAdvisoryResult({ advice }: { advice: AIAdviceResponse }) {
  // AI unavailable (AI down/timeout/circuit breaker OPEN) — Backend đã trả 200 kèm message
  // theo đúng contract "hệ thống chính vẫn hoạt động" (Task 6.18). Frontend CHỈ hiển thị lại,
  // không coi đây là lỗi (không dùng <ErrorMessage>) vì nghiệp vụ chính (Ticket/Quote) không hề bị ảnh hưởng.
  if (!advice.aiAvailable) {
    return (
      <div className={styles.unavailableBox}>
        <span>⚠ {advice.message}</span>
      </div>
    );
  }

  // Trường hợp SUCCESS nhưng rỗng cả 2 danh sách (NO_MATCH/OUT_OF_SCOPE đã được Handler map
  // thành aiAvailable=true kèm message riêng — xem Task 6.12) hoặc dữ liệu bị validate loại hết.
  if (advice.suggestedServices.length === 0 && advice.suggestedParts.length === 0) {
    return (
      <div className={styles.noMatchBox}>
        <span>
          {advice.message ??
            'AI chưa đủ dữ liệu để tư vấn, vui lòng mang máy đến kiểm tra trực tiếp.'}
        </span>
      </div>
    );
  }

  const priceMin = Math.min(
    ...advice.suggestedServices.map((s) => s.priceRangeMin).filter((p) => p > 0)
  );
  const priceMax = Math.max(
    ...advice.suggestedServices.map((s) => s.priceRangeMax).filter((p) => p > 0)
  );
  const hasPriceRange = Number.isFinite(priceMin) && Number.isFinite(priceMax) && priceMax > 0;

  return (
    <div className={styles.resultBox}>
      <h4 className={styles.resultTitle}>🤖 Gợi ý AI</h4>

      {advice.suggestedServices.length > 0 && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Dịch vụ:</span>
          <ul className={styles.checkList}>
            {advice.suggestedServices.map((s) => (
              <li key={s.serviceId}>✓ {s.serviceName}</li>
            ))}
          </ul>
        </div>
      )}

      {advice.suggestedParts.length > 0 && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Linh kiện:</span>
          <ul className={styles.checkList}>
            {advice.suggestedParts.map((part, i) => (
              <li key={i}>✓ {part}</li>
            ))}
          </ul>
        </div>
      )}

      {hasPriceRange && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Khoảng giá:</span>
          <p className={styles.priceRange}>
            {priceMin.toLocaleString('vi-VN')} - {priceMax.toLocaleString('vi-VN')} VNĐ
          </p>
        </div>
      )}

      {advice.explanation && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Lý do:</span>
          <p>{advice.explanation}</p>
        </div>
      )}

      {/* Disclaimer BẮT BUỘC theo contract (Task 5 Tuần 2) — luôn hiển thị, không có điều kiện ẩn nào */}
      <div className={styles.disclaimer}>⚠ {advice.disclaimer}</div>
    </div>
  );
}
