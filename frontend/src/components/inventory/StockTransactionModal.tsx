import { useState } from 'react';
import { Modal, Button, Select, Input, ErrorMessage } from '../common';
import { inventoryService } from '../../services/inventoryService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { Part } from '../../types/inventory.types';

interface Props { isOpen: boolean; part: Part | null; onClose: () => void; onDone: () => void; }

export default function StockTransactionModal({ isOpen, part, onClose, onDone }: Props) {
  const { showSuccess } = useToast();
  const [type, setType] = useState<'Import' | 'Adjustment'>('Import');
  const [quantity, setQuantity] = useState(1);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async () => {
    if (!part) return;
    setErrorMessage(null);
    setIsSaving(true);
    try {
      await inventoryService.createTransaction({ partId: part.id, type, quantity });
      showSuccess(type === 'Import' ? `Đã nhập ${quantity} ${part.unit} ${part.name}.` : `Đã điều chỉnh tồn kho.`);
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Nhập/Điều chỉnh kho — ${part?.name ?? ''}`}
      footer={<><Button variant="secondary" onClick={onClose} disabled={isSaving}>Hủy</Button><Button onClick={handleSubmit} isLoading={isSaving}>Xác nhận</Button></>}>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <p style={{ marginBottom: 12, color: 'var(--color-text-muted)', fontSize: 14 }}>
        Tồn hiện tại: <strong>{part?.quantityOnHand}</strong> {part?.unit}
      </p>
      <Select label="Loại giao dịch" value={type}
        options={[{ value: 'Import', label: 'Nhập kho' }, { value: 'Adjustment', label: 'Điều chỉnh tăng' }]}
        onChange={(e) => setType(e.target.value as 'Import' | 'Adjustment')} />
      <Input label="Số lượng" type="number" min={1} value={quantity}
        onChange={(e) => setQuantity(Number(e.target.value))} />
    </Modal>
  );
}