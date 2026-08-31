import { type FormEvent, useEffect, useState } from 'react';
import { Button, Input, Select, ErrorMessage, Loading } from '../common';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import type { Customer } from '../../types/customer.types';
import type { Device, DeviceType } from '../../types/device.types';
import styles from './IntakeStep.module.css';

const DEVICE_TYPE_OPTIONS = [
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

interface IntakeDeviceStepProps {
  customer: Customer;
  onBack: () => void;
  onNext: (device: Device) => void;
}

export default function IntakeDeviceStep({ customer, onBack, onNext }: IntakeDeviceStepProps) {
  const [existingDevices, setExistingDevices] = useState<Device[]>([]);
  const [isLoadingDevices, setIsLoadingDevices] = useState(true);
  const [mode, setMode] = useState<'existing' | 'new'>('new');

  const [form, setForm] = useState({
    deviceType: 'Phone' as DeviceType,
    brand: '',
    model: '',
    serialNumber: '',
  });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    deviceService
      .getByCustomerId(customer.id)
      .then((devices) => {
        setExistingDevices(devices);
        if (devices.length > 0) setMode('existing');
      })
      .finally(() => setIsLoadingDevices(false));
  }, [customer.id]);

  const handleCreateNew = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);
    try {
      const device = await deviceService.create({
        customerId: customer.id,
        deviceType: form.deviceType,
        brand: form.brand,
        model: form.model,
        serialNumber: form.serialNumber || undefined,
      });
      onNext(device);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div>
      <div className={styles.selectedCard}>
        <span>
          Khách hàng: <strong>{customer.fullName}</strong> ({customer.phone})
        </span>
      </div>

      {isLoadingDevices ? (
        <Loading message="Đang tải thiết bị của khách hàng..." />
      ) : (
        <>
          {existingDevices.length > 0 && (
            <div className={styles.toggle}>
              <button
                className={`${styles.toggleBtn} ${mode === 'existing' ? styles.toggleActive : ''}`}
                onClick={() => setMode('existing')}
              >
                Thiết bị đã có
              </button>
              <button
                className={`${styles.toggleBtn} ${mode === 'new' ? styles.toggleActive : ''}`}
                onClick={() => setMode('new')}
              >
                + Thiết bị mới
              </button>
            </div>
          )}

          {mode === 'existing' && existingDevices.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {existingDevices.map((d) => (
                <div key={d.id} className={styles.deviceOption} onClick={() => onNext(d)}>
                  <strong>
                    {d.brand} {d.model}
                  </strong>{' '}
                  — {d.serialNumber ?? 'Không có Serial'}
                </div>
              ))}
            </div>
          ) : (
            <form onSubmit={handleCreateNew} className={styles.form}>
              {errorMessage && <ErrorMessage message={errorMessage} />}
              <Select
                label="Loại thiết bị"
                options={DEVICE_TYPE_OPTIONS}
                value={form.deviceType}
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
                label="Số Serial/IMEI"
                value={form.serialNumber}
                onChange={(e) => setForm({ ...form, serialNumber: e.target.value })}
              />
              <Button type="submit" isLoading={isSaving}>
                Lưu thiết bị & Tiếp tục
              </Button>
            </form>
          )}
        </>
      )}

      <div className={styles.actions}>
        <Button variant="secondary" onClick={onBack}>
          ← Quay lại
        </Button>
      </div>
    </div>
  );
}
