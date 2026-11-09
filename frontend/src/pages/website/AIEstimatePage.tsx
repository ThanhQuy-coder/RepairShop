import { type FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { aiAdvisoryService } from '../../services/aiAdvisoryService';
import { extractApiError } from '../../utils/apiError';
import type { AIAdviceResponse } from '../../types/aiAdvisory.types';
import { DEVICE_TYPE_OPTIONS, type DeviceType } from '../../types/device.types';
import { useDeviceCatalogCascade } from '../../hooks/useDeviceCatalogCascade';
import { Button, Input, Select, ErrorMessage } from '../../components/common';
import styles from './AIEstimatePage.module.css';

const DRAFT_STORAGE_KEY = 'ai-estimate-draft';

export default function AIEstimatePage() {
  const navigate = useNavigate();
  const { isAuthenticated, role } = useAuth();

  const [deviceType, setDeviceType] = useState<DeviceType>('Phone');
  const [brandId, setBrandId] = useState('');
  const [deviceModelId, setDeviceModelId] = useState('');
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [issueDescription, setIssueDescription] = useState('');
  const [advice, setAdvice] = useState<AIAdviceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    brands,
    models,
    loadingBrands: isLoadingBrands,
    loadingModels: isLoadingModels,
  } = useDeviceCatalogCascade(deviceType, brandId);

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

  const canUseAI = isAuthenticated && (role === 'Customer' || role === 'Receptionist' || role === 'Admin');

  const handleDeviceTypeChange = (nextType: DeviceType) => {
    setDeviceType(nextType);
    setBrandId('');
    setDeviceModelId('');
    setBrand('');
    setModel('');
  };

  const handleBrandSelect = (nextBrandId: string) => {
    setBrandId(nextBrandId);
    setDeviceModelId('');
    const found = brands.find((b) => b.id === nextBrandId);
    setBrand(found ? found.name : '');
    setModel('');
  };

  const handleModelSelect = (nextModelId: string) => {
    setDeviceModelId(nextModelId);
    const found = models.find((m) => m.id === nextModelId);
    if (found) {
      setModel(found.modelName);
    }
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!brand.trim() || !model.trim() || !issueDescription.trim()) {
      setErrorMessage('Vui lòng chọn hoặc điền đầy đủ thông tin thiết bị và mô tả tình trạng.');
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
        <h2>Tư vấn & Dự toán chi phí sửa chữa bằng AI — Fixora</h2>
        <p>
          Chọn dòng thiết bị và mô tả triệu chứng để nhận gợi ý dịch vụ cùng chi phí tham khảo trước khi mang máy đến Fixora.{' '}
          <Link to="/consult">
            Chuyển sang Trợ lý AI MAS mới (Chẩn đoán, Báo giá & Đặt lịch không cần đăng nhập) →
          </Link>
        </p>
      </div>

      <form onSubmit={handleSubmit} className={styles.form}>
        <Select
          label="1. Loại thiết bị"
          options={DEVICE_TYPE_OPTIONS}
          value={deviceType}
          onChange={(event) => handleDeviceTypeChange(event.target.value as DeviceType)}
        />

        <div className={styles.row}>
          <Select
            label="2. Hãng sản xuất"
            value={brandId}
            onChange={(event) => handleBrandSelect(event.target.value)}
            disabled={isLoadingBrands}
            options={[
              {
                value: '',
                label: isLoadingBrands ? 'Đang tải hãng...' : '-- Chọn hãng sản xuất --',
              },
              ...brands.map((b) => ({ value: b.id, label: b.name })),
            ]}
          />
          <Select
            label="3. Dòng máy (Model)"
            value={deviceModelId}
            onChange={(event) => handleModelSelect(event.target.value)}
            disabled={!brandId || isLoadingModels}
            options={[
              {
                value: '',
                label: !brandId
                  ? '-- Chọn hãng trước --'
                  : isLoadingModels
                  ? 'Đang tải dòng máy...'
                  : '-- Chọn dòng máy --',
              },
              ...models.map((m) => ({ value: m.id, label: m.modelName })),
            ]}
          />
        </div>

        <div className={styles.row}>
          <Input
            label="Hoặc nhập tên Hãng"
            value={brand}
            onChange={(event) => setBrand(event.target.value)}
            placeholder="VD: Apple, Samsung, Dell..."
          />
          <Input
            label="Hoặc nhập tên Dòng máy"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            placeholder="VD: iPhone 15 Pro Max, MacBook Air M2..."
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label}>Mô tả tình trạng lỗi</label>
          <textarea
            className={styles.textarea}
            rows={4}
            maxLength={500}
            value={issueDescription}
            onChange={(event) => setIssueDescription(event.target.value)}
            placeholder="VD: Màn hình bị sọc xanh, pin tụt nhanh và nóng máy khi sạc..."
          />
          <span className={styles.charCount}>{issueDescription.length}/500</span>
        </div>

        {errorMessage && <ErrorMessage message={errorMessage} />}

        <Button type="submit" isLoading={isLoading} className={styles.submitButton}>
          {canUseAI ? '🤖 Nhận tư vấn AI từ Fixora' : 'Đăng nhập để xem gợi ý'}
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
          <span>AI Fixora đang phân tích triệu chứng...</span>
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
          'AI chưa đủ dữ liệu để tư vấn, vui lòng mang máy đến Fixora để kiểm tra trực tiếp miễn phí.'}
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
      <h4>🤖 Gợi ý tham khảo từ Fixora AI</h4>

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
          <span className={styles.resultLabel}>Lý do chẩn đoán:</span>
          <p>{advice.explanation}</p>
        </div>
      )}

      <div className={styles.disclaimer}>⚠ {advice.disclaimer}</div>
    </div>
  );
}
