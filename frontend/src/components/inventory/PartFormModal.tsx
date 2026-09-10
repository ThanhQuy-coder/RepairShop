import { type FormEvent, useEffect, useState } from 'react';
import { Modal, Button, Input, Select, ErrorMessage } from '../common';
import { partService } from '../../services/partService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { Part } from '../../types/inventory.types';

const DEVICE_TYPE_OPTIONS = [
  { value: '', label: 'Dùng chung mọi loại' },
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

interface PartFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaved: () => void;
  editingPart?: Part | null;
}

export default function PartFormModal({
  isOpen,
  onClose,
  onSaved,
  editingPart,
}: PartFormModalProps) {
  const { showSuccess } = useToast();
  const [form, setForm] = useState({
    name: '',
    sku: '',
    costPrice: 0,
    unitPrice: 0,
    category: '',
    compatibleDeviceType: '',
    unit: 'cái',
    minStockThreshold: 0,
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setForm({
        name: editingPart?.name ?? '',
        sku: editingPart?.sku ?? '',
        costPrice: editingPart?.costPrice ?? 0,
        unitPrice: editingPart?.unitPrice ?? 0,
        category: editingPart?.category ?? '',
        compatibleDeviceType: editingPart?.compatibleDeviceType ?? '',
        unit: editingPart?.unit ?? 'cái',
        minStockThreshold: editingPart?.minStockThreshold ?? 0,
      });
      setErrorMessage(null);
    }
  }, [isOpen, editingPart]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);

    try {
      const payload = {
        name: form.name,
        costPrice: form.costPrice,
        unitPrice: form.unitPrice,
        category: form.category || undefined,
        compatibleDeviceType: form.compatibleDeviceType || undefined,
        unit: form.unit,
        minStockThreshold: form.minStockThreshold,
      };

      if (editingPart) {
        await partService.update(editingPart.id, payload);
        showSuccess('Đã cập nhật linh kiện.');
      } else {
        await partService.create({ ...payload, sku: form.sku });
        showSuccess('Đã thêm linh kiện mới.');
      }

      onSaved();
      onClose();
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
      title={editingPart ? 'Sửa linh kiện' : 'Thêm linh kiện mới'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} isLoading={isSaving}>
            Lưu
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        {errorMessage && <ErrorMessage message={errorMessage} />}

        <Input
          label="Mã SKU"
          required
          disabled={!!editingPart}
          value={form.sku}
          onChange={(e) => setForm({ ...form, sku: e.target.value })}
        />
        <Input
          label="Tên linh kiện"
          required
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />
        <Input
          label="Danh mục"
          value={form.category}
          onChange={(e) => setForm({ ...form, category: e.target.value })}
          placeholder="VD: Pin, Màn hình..."
        />
        <Select
          label="Tương thích thiết bị"
          options={DEVICE_TYPE_OPTIONS}
          value={form.compatibleDeviceType}
          onChange={(e) => setForm({ ...form, compatibleDeviceType: e.target.value })}
        />

        <div style={{ display: 'flex', gap: 8 }}>
          <Input
            label="Giá nhập"
            type="number"
            min={0}
            value={form.costPrice}
            onChange={(e) => setForm({ ...form, costPrice: Number(e.target.value) })}
          />
          <Input
            label="Giá bán"
            type="number"
            min={0}
            value={form.unitPrice}
            onChange={(e) => setForm({ ...form, unitPrice: Number(e.target.value) })}
          />
        </div>

        <div style={{ display: 'flex', gap: 8 }}>
          <Input
            label="Đơn vị"
            value={form.unit}
            onChange={(e) => setForm({ ...form, unit: e.target.value })}
          />
          <Input
            label="Ngưỡng tồn kho tối thiểu"
            type="number"
            min={0}
            value={form.minStockThreshold}
            onChange={(e) => setForm({ ...form, minStockThreshold: Number(e.target.value) })}
          />
        </div>
      </form>
    </Modal>
  );
}
