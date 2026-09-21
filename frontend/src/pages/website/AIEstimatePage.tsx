import { type FormEvent, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { aiAdvisoryService } from '../../services/aiAdvisoryService';
import { extractApiError } from '../../utils/apiError';
import type { AIAdviceResponse } from '../../types/aiAdvisory.types';
import type { DeviceType } from '../../types/device.types';
import { Button, Input, Select, ErrorMessage } from '../../components/common';
import styles from './AIEstimatePage.module.css';

const DEVICE_TYPE_OPTIONS = [
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

const DRAFT_STORAGE_KEY = 'ai-estimate-draft';

export default function AIEstimatePage() {
  const navigate = useNavigate();
  const { isAuthenticated, role } = useAuth();

  const [deviceType, setDeviceType] = useState<DeviceType>('Phone');
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [issueDescription, setIssueDescription] = useState('');
  const [advice, setAdvice] = useState<AIAdviceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    const draft = sessionStorage.getItem(DRAFT_STORAGE_KEY);
    if (!draft) return;

    try {
      const parsed = JSON.parse(draft) as {
        deviceType?: DeviceType;
        brand?: string;
        model?: string;
        issueDescription?: string;
      };

      if (parsed.deviceType) setDeviceType(parsed.deviceType);
      if (parsed.brand) setBrand(parsed.brand);
      if (parsed.model) setModel(parsed.model);
      if (parsed.issueDescription) setIssueDescription(parsed.issueDescription);
    } catch {
      // ignore invalid JSON
    } finally {
      sessionStorage.removeItem(DRAFT_STORAGE_KEY);
    }
  }, [isAuthenticated]);

  const canUseAI = isAuthenticated && (role === 'Customer' || role === 'Receptionist');

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!brand.trim() || !model.trim() || !issueDescription.trim()) {
      setErrorMessage('Vui lòng điền đầy đủ thông tin thiết bị và mô tả tình trạng.');
      return;
    }

    if (!canUseAI) {
      sessionStorage.setItem(
        DRAFT_STORAGE_KEY,
        JSON.stringify({ deviceType, brand, model, issueDescription })
      );
      sessionStorage.setItem('post-login-redirect', '/estimate');
      navigate('/login');
      return;
    }

    setErrorMessage(null);
    setAdvice(null);
    setIsLoading(true);

    try {
      const result = await aiAdvisoryService.getAdvice({
        deviceType,
        brand: brand.trim(),
        model: model.trim(),
        issueDescription: issueDescription.trim(),
      });
      setAdvice(result);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.intro}>
        <h2>Tư vấn giá sửa chữa bằng AI</h2>
        <p>Mô tả thiết bị và tình trạng để nhận gợi ý trước khi mang máy đến cửa hàng.</p>
      </div>

      <form onSubmit={handleSubmit} className={styles.form}>
        <Select
          label="Loại thiết bị"
          options={DEVICE_TYPE_OPTIONS}
          value={deviceType}
          onChange={(event) => setDeviceType(event.target.value as DeviceType)}
        />

        <div className={styles.row}>
          <Input
            label="Hãng"
            value={brand}
            onChange={(event) => setBrand(event.target.value)}
            placeholder="VD: iPhone"
          />
          <Input
            label="Model"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            placeholder="VD: 13"
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label}>Mô tả tình trạng</label>
          <textarea
            className={styles.textarea}
            rows={4}
            maxLength={500}
            value={issueDescription}
            onChange={(event) => setIssueDescription(event.target.value)}
            placeholder="VD: Pin tụt nhanh, máy nóng khi sạc..."
          />
          <span className={styles.charCount}>{issueDescription.length}/500</span>
        </div>

        {errorMessage && <ErrorMessage message={errorMessage} />}

        <Button type="submit" isLoading={isLoading} className={styles.submitButton}>
          {canUseAI ? '🤖 Nhận tư vấn AI' : 'Đăng nhập để xem gợi ý'}
        </Button>

        {!isAuthenticated && (
          <p className={styles.guestHint}>
            Bạn có thể điền thử ngay — chỉ cần đăng nhập ở bước cuối để xem kết quả.
          </p>
        )}
      </form>

      {isLoading && (
        <div className={styles.loadingBox}>
          <div className={styles.spinner} />
          <span>AI đang phân tích...</span>
        </div>
      )}

      {!isLoading && advice && <AIEstimateResult advice={advice} />}
    </div>
  );
}

function AIEstimateResult({ advice }: { advice: AIAdviceResponse }) {
  if (!advice.aiAvailable) {
    return <div className={styles.unavailableBox}>⚠ {advice.message}</div>;
  }

  if (advice.suggestedServices.length === 0 && advice.suggestedParts.length === 0) {
    return (
      <div className={styles.noMatchBox}>
        {advice.message ??
          'AI chưa đủ dữ liệu để tư vấn, vui lòng mang máy đến kiểm tra trực tiếp.'}
      </div>
    );
  }

  const prices = advice.suggestedServices
    .flatMap((service) => [service.priceRangeMin, service.priceRangeMax])
    .filter((price) => price > 0);

  const priceMin = prices.length ? Math.min(...prices) : null;
  const priceMax = prices.length ? Math.max(...prices) : null;

  return (
    <div className={styles.resultBox}>
      <h4>🤖 Gợi ý tham khảo</h4>

      {advice.suggestedServices.length > 0 && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Dịch vụ có thể cần:</span>
          <ul className={styles.checkList}>
            {advice.suggestedServices.map((service) => (
              <li key={service.serviceId}>✓ {service.serviceName}</li>
            ))}
          </ul>
        </div>
      )}

      {advice.suggestedParts.length > 0 && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Linh kiện có thể cần thay:</span>
          <ul className={styles.checkList}>
            {advice.suggestedParts.map((part, index) => (
              <li key={`${part}-${index}`}>✓ {part}</li>
            ))}
          </ul>
        </div>
      )}

      {priceMin !== null && priceMax !== null && (
        <div className={styles.resultSection}>
          <span className={styles.resultLabel}>Khoảng giá tham khảo:</span>
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

      <div className={styles.disclaimer}>⚠ {advice.disclaimer}</div>
    </div>
  );
}
