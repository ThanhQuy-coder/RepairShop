import { type FormEvent, useEffect, useState } from 'react';
import { Modal, Button, Input, Select, ErrorMessage } from '../common';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import { DEVICE_TYPE_OPTIONS, type Device, type DeviceType } from '../../types/device.types';
import { useToast } from '../../hooks/useToast';
import { useDeviceCatalogCascade } from '../../hooks/useDeviceCatalogCascade';

interface DeviceFormModalProps {
  isOpen: boolean;
  customerId: string;
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
    deviceModelId: '' as string,
    serialNumber: '',
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  const { brands, models, loadingBrands, loadingModels } = useDeviceCatalogCascade(
    form.deviceType,
    form.brand
  );

  useEffect(() => {
    if (isOpen) {
      setForm({
        deviceType: editingDevice?.deviceType ?? 'Phone',
        brand: editingDevice?.brand ?? '',
        model: editingDevice?.model ?? '',
        deviceModelId: editingDevice?.deviceModelId ?? '',
        serialNumber: editingDevice?.serialNumber ?? '',
      });
      setErrorMessage(null);
      setErrors([]);
    }
  }, [isOpen, editingDevice]);

  const handleDeviceTypeChange = (nextType: DeviceType) => {
    setForm((prev) => ({
      ...prev,
      deviceType: nextType,
      brand: '',
      model: '',
      deviceModelId: '',
    }));
  };

  const handleBrandChange = (nextBrand: string) => {
    setForm((prev) => ({
      ...prev,
      brand: nextBrand,
      model: '',
      deviceModelId: '',
    }));
  };

  const handleModelSelect = (nextModelName: string) => {
    const matched = models.find((m) => m.modelName === nextModelName);
    setForm((prev) => ({
      ...prev,
      model: nextModelName,
      deviceModelId: matched?.id ?? '',
    }));
  };

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
            deviceModelId: form.deviceModelId || null,
          })
        : await deviceService.create({
            customerId,
            deviceType: form.deviceType,
            brand: form.brand,
            model: form.model,
            serialNumber: form.serialNumber || undefined,
            deviceModelId: form.deviceModelId || null,
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

  const brandOptions = [
    {
      value: '',
      label: loadingBrands ? 'Đang tải hãng sản xuất...' : '-- Chọn hãng sản xuất --',
    },
    ...brands.map((b) => ({ value: b.name, label: b.name })),
    ...(form.brand && !brands.some((b) => b.name === form.brand)
      ? [{ value: form.brand, label: `${form.brand} (Hiện tại)` }]
      : []),
  ];

  const modelOptions = [
    {
      value: '',
      label: !form.brand
        ? '-- Chọn hãng trước --'
        : loadingModels
          ? 'Đang tải dòng máy...'
          : '-- Chọn dòng máy (Model) --',
    },
    ...models.map((m) => ({
      value: m.modelName,
      label: m.modelNumber ? `${m.modelName} (${m.modelNumber})` : m.modelName,
    })),
    ...(form.model && !models.some((m) => m.modelName === form.model)
      ? [{ value: form.model, label: `${form.model} (Hiện tại)` }]
      : []),
  ];

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
          label="1. Loại thiết bị"
          options={DEVICE_TYPE_OPTIONS}
          value={form.deviceType}
          disabled={!!editingDevice}
          onChange={(e) => handleDeviceTypeChange(e.target.value as DeviceType)}
        />

        <Select
          label="2. Hãng sản xuất"
          options={brandOptions}
          value={form.brand}
          disabled={!form.deviceType || loadingBrands}
          onChange={(e) => handleBrandChange(e.target.value)}
        />

        <Select
          label="3. Dòng máy (Model)"
          options={modelOptions}
          value={form.model}
          disabled={!form.brand || loadingModels}
          onChange={(e) => handleModelSelect(e.target.value)}
        />

        <Input
          label="Số Serial / IMEI"
          placeholder="Nhập Serial hoặc IMEI (nếu có)"
          value={form.serialNumber}
          onChange={(e) => setForm({ ...form, serialNumber: e.target.value })}
        />
      </form>
    </Modal>
  );
}
