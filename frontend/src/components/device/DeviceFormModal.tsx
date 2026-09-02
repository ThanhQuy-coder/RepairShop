import { type FormEvent, useEffect, useState } from 'react';
import { Modal, Button, Input, Select, ErrorMessage } from '../common';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import type { Device, DeviceType } from '../../types/device.types';
import { useToast } from '../../hooks/useToast';

const DEVICE_TYPE_OPTIONS = [
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

interface DeviceFormModalProps {
  isOpen: boolean;
  customerId: string; // Device luôn thuộc 1 Customer xác định — bắt buộc phải có
  onClose: () => void;
  onSaved: (device: Device) => void;
  editingDevice?: Device | null;
}

export default function DeviceFormModal({
  isOpen,
  customerId,
  onClose,
  onSaved,
  editingDevice,
}: DeviceFormModalProps) {
  const [form, setForm] = useState({
    deviceType: 'Phone' as DeviceType,
    brand: '',
    model: '',
    serialNumber: '',
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  useEffect(() => {
    if (isOpen) {
      setForm({
        deviceType: editingDevice?.deviceType ?? 'Phone',
        brand: editingDevice?.brand ?? '',
        model: editingDevice?.model ?? '',
        serialNumber: editingDevice?.serialNumber ?? '',
      });
      setErrorMessage(null);
      setErrors([]);
    }
  }, [isOpen, editingDevice]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);

    try {
      const result = editingDevice
        ? await deviceService.update(editingDevice.id, {
            brand: form.brand,
            model: form.model,
            serialNumber: form.serialNumber || undefined,
          })
        : await deviceService.create({
            customerId,
            deviceType: form.deviceType,
            brand: form.brand,
            model: form.model,
            serialNumber: form.serialNumber || undefined,
          });

      onSaved(result);
      showSuccess(editingDevice ? 'Đã cập nhật thiết bị.' : 'Đã thêm thiết bị mới.');
      onClose();
    } catch (err) {
      const apiError = extractApiError(err);
      setErrorMessage(apiError.message);
      setErrors(apiError.errors);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={editingDevice ? 'Sửa thông tin thiết bị' : 'Thêm thiết bị mới'}
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
        {errorMessage && <ErrorMessage message={errorMessage} errors={errors} />}

        <Select
          label="Loại thiết bị"
          options={DEVICE_TYPE_OPTIONS}
          value={form.deviceType}
          disabled={!!editingDevice} // đổi loại thiết bị sau khi tạo là bất thường nghiệp vụ — khoá lại khi Edit
          onChange={(e) => setForm({ ...form, deviceType: e.target.value as DeviceType })}
        />

        <Input
          label="Hãng"
          required
          value={form.brand}
          onChange={(e) => setForm({ ...form, brand: e.target.value })}
        />
        <Input
          label="Model"
          required
          value={form.model}
          onChange={(e) => setForm({ ...form, model: e.target.value })}
        />
        <Input
          label="Số Serial / IMEI"
          value={form.serialNumber}
          onChange={(e) => setForm({ ...form, serialNumber: e.target.value })}
        />
      </form>
    </Modal>
  );
}
