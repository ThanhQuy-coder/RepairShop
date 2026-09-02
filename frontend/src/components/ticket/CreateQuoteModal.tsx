import { useState } from 'react';
import { Modal, Button, Input, Select, ErrorMessage } from '../common';
import { quoteService } from '../../services/quoteService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface QuoteItemForm {
  itemType: 'Service' | 'Part';
  description: string;
  quantity: number;
  unitPrice: number;
}

interface CreateQuoteModalProps {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onCreated: () => void;
}

export default function CreateQuoteModal({
  isOpen,
  ticketId,
  onClose,
  onCreated,
}: CreateQuoteModalProps) {
  const [description, setDescription] = useState('');
  const [items, setItems] = useState<QuoteItemForm[]>([
    { itemType: 'Service', description: '', quantity: 1, unitPrice: 0 },
  ]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  const updateItem = (index: number, patch: Partial<QuoteItemForm>) =>
    setItems((prev) => prev.map((it, i) => (i === index ? { ...it, ...patch } : it)));

  const addItem = () =>
    setItems((prev) => [
      ...prev,
      { itemType: 'Service', description: '', quantity: 1, unitPrice: 0 },
    ]);
  const removeItem = (index: number) => setItems((prev) => prev.filter((_, i) => i !== index));

  const total = items.reduce((sum, it) => sum + it.quantity * it.unitPrice, 0);

  const handleSubmit = async () => {
    setErrorMessage(null);
    setIsSaving(true);
    try {
      await quoteService.create(ticketId, { description, items });

      showSuccess('Đã tạo báo giá, chờ khách hàng xác nhận.');
      onCreated();
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
      title="Tạo báo giá"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Tạo báo giá ({total.toLocaleString('vi-VN')}đ)
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <Input
        label="Mô tả báo giá"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />

      {items.map((item, i) => (
        <div key={i} style={{ display: 'flex', gap: 8, marginTop: 12, alignItems: 'flex-end' }}>
          <Select
            options={[
              { value: 'Service', label: 'Dịch vụ' },
              { value: 'Part', label: 'Linh kiện' },
            ]}
            value={item.itemType}
            onChange={(e) => updateItem(i, { itemType: e.target.value as 'Service' | 'Part' })}
          />
          <Input
            placeholder="Mô tả"
            value={item.description}
            onChange={(e) => updateItem(i, { description: e.target.value })}
          />
          <Input
            type="number"
            min={1}
            style={{ width: 70 }}
            value={item.quantity}
            onChange={(e) => updateItem(i, { quantity: Number(e.target.value) })}
          />
          <Input
            type="number"
            min={0}
            style={{ width: 110 }}
            value={item.unitPrice}
            onChange={(e) => updateItem(i, { unitPrice: Number(e.target.value) })}
          />
          {items.length > 1 && (
            <Button type="button" variant="ghost" size="sm" onClick={() => removeItem(i)}>
              ×
            </Button>
          )}
        </div>
      ))}

      <Button type="button" variant="ghost" size="sm" onClick={addItem} style={{ marginTop: 8 }}>
        + Thêm hạng mục
      </Button>
    </Modal>
  );
}
