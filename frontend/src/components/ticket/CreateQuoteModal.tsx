import { useEffect, useState } from 'react';
import { Modal, Button, Input, Select, ErrorMessage } from '../common';
import { quoteService } from '../../services/quoteService';
import { contentService } from '../../services/contentService';
import { partService } from '../../services/partService';
import type { ServiceItem } from '../../types/content.types';
import type { Part } from '../../types/inventory.types';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface QuoteItemForm {
  itemType: 'Service' | 'Part';
  catalogId: string;
  quantity: number;
}

interface CreateQuoteModalProps {
  isOpen: boolean;
  ticketId: string;
  deviceType?: string;
  onClose: () => void;
  onCreated: () => void;
}

export default function CreateQuoteModal({
  isOpen,
  ticketId,
  deviceType,
  onClose,
  onCreated,
}: CreateQuoteModalProps) {
  const [description, setDescription] = useState('');
  const [items, setItems] = useState<QuoteItemForm[]>([
    { itemType: 'Service', catalogId: '', quantity: 1 },
  ]);
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [parts, setParts] = useState<Part[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  useEffect(() => {
    if (!isOpen) return;
    Promise.all([contentService.getPublicServices(), partService.list({ page: 1, pageSize: 100 })])
      .then(([serviceResult, partResult]) => {
        setServices(serviceResult.items.filter((service) => service.isActive));
        setParts(partResult.items.filter((part) => part.isActive));
      })
      .catch((err) => setErrorMessage(extractApiError(err).message));
  }, [isOpen]);

  const updateItem = (index: number, patch: Partial<QuoteItemForm>) =>
    setItems((prev) => prev.map((item, i) => (i === index ? { ...item, ...patch } : item)));

  const addItem = () =>
    setItems((prev) => [...prev, { itemType: 'Service', catalogId: '', quantity: 1 }]);
  const removeItem = (index: number) => setItems((prev) => prev.filter((_, i) => i !== index));

  const getPrice = (item: QuoteItemForm) => {
    if (item.itemType === 'Service') {
      return services.find((service) => service.id === item.catalogId)?.basePrice ?? 0;
    }
    return parts.find((part) => part.id === item.catalogId)?.unitPrice ?? 0;
  };
  const total = items.reduce((sum, item) => sum + item.quantity * getPrice(item), 0);

  const handleSubmit = async () => {
    setErrorMessage(null);
    if (items.some((item) => !item.catalogId)) {
      setErrorMessage('Vui lòng chọn dịch vụ hoặc linh kiện cho từng hạng mục.');
      return;
    }
    setIsSaving(true);
    try {
      await quoteService.create(ticketId, {
        description,
        items: items.map((item) => ({
          itemType: item.itemType,
          quantity: item.quantity,
          ...(item.itemType === 'Service'
            ? { serviceId: item.catalogId }
            : { partId: item.catalogId }),
        })),
      });
      showSuccess('Đã tạo báo giá, chờ khách hàng xác nhận.');
      onCreated();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Tạo báo giá" footer={
      <>
        <Button variant="secondary" onClick={onClose} disabled={isSaving}>Hủy</Button>
        <Button onClick={handleSubmit} isLoading={isSaving}>
          Tạo báo giá ({total.toLocaleString('vi-VN')}đ)
        </Button>
      </>
    }>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <Input label="Mô tả báo giá" value={description}
        onChange={(e) => setDescription(e.target.value)} />
      {items.map((item, index) => {
        const options = item.itemType === 'Service'
          ? services
              .filter((service) => !service.deviceType || !deviceType ||
                service.deviceType.toLowerCase() === deviceType.toLowerCase())
              .map((service) => ({
                value: service.id,
                label: `${service.name} (${(service.basePrice ?? 0).toLocaleString('vi-VN')}đ)`,
              }))
          : parts.map((part) => ({
              value: part.id,
              label: `${part.name} (${part.unitPrice.toLocaleString('vi-VN')}đ)`,
            }));
        return (
          <div key={index} style={{ display: 'flex', gap: 8, marginTop: 12, alignItems: 'flex-end', flexWrap: 'wrap' }}>
            <Select options={[
              { value: 'Service', label: 'Dịch vụ' },
              { value: 'Part', label: 'Linh kiện' },
            ]} value={item.itemType}
              onChange={(e) => updateItem(index, {
                itemType: e.target.value as 'Service' | 'Part', catalogId: '',
              })} />
            <Select options={options} value={item.catalogId}
              onChange={(e) => updateItem(index, { catalogId: e.target.value })} />
            <Input type="number" min={1} style={{ width: 70 }} value={item.quantity}
              onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })} />
            <span>{getPrice(item).toLocaleString('vi-VN')}đ</span>
            {items.length > 1 && <Button type="button" variant="ghost" size="sm"
              onClick={() => removeItem(index)}>×</Button>}
          </div>
        );
      })}
      <Button type="button" variant="ghost" size="sm" onClick={addItem} style={{ marginTop: 8 }}>
        + Thêm hạng mục
      </Button>
    </Modal>
  );
}
