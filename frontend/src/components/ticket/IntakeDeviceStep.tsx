import { type FormEvent, useEffect, useState } from 'react';
import { Button, Input, Select, ErrorMessage, Loading } from '../common';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import type { Device, DeviceType } from '../../types/device.types';
import styles from './IntakeStep.module.css';
import { useTicketIntakeStore } from '../../store/ticketIntakeStore';

const DEVICE_TYPE_OPTIONS = [
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

export default function IntakeDeviceStep() {
  // Lấy dữ liệu và actions trực tiếp từ Zustand.
  const customer = useTicketIntakeStore((s) => s.customer);
  const appointmentData = useTicketIntakeStore((s) => s.appointmentData);
  const selectedDevice = useTicketIntakeStore((s) => s.device);
  const setDevice = useTicketIntakeStore((s) => s.setDevice);
  const setStep = useTicketIntakeStore((s) => s.setStep);

  const [existingDevices, setExistingDevices] = useState<Device[]>([]);
  const [isLoadingDevices, setIsLoadingDevices] = useState(true);
  const [mode, setMode] = useState<'existing' | 'new'>('new');

  const [form, setForm] = useState({
    deviceType: (appointmentData?.deviceType ?? 'Phone') as DeviceType,
    brand: appointmentData?.brand ?? '',
    model: appointmentData?.model ?? '',
    serialNumber: appointmentData?.serialNumber ?? '',
  });

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    // Chưa có khách hàng thì không gọi API.
    if (!customer) {
      setExistingDevices([]);
      setIsLoadingDevices(false);
      return;
    }

    let cancelled = false;

    setIsLoadingDevices(true);

    deviceService
      .getByCustomerId(customer.id)
      .then((devices) => {
        if (cancelled) return;

        setExistingDevices(devices);

        if (devices.length > 0) {
          setMode('existing');
        } else {
          setMode('new');
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setErrorMessage(extractApiError(err).message);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoadingDevices(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [customer?.id]);

  const handleCreateNew = async (e: FormEvent) => {
    e.preventDefault();

    if (!customer) {
      setErrorMessage('Vui lòng chọn khách hàng trước.');
      return;
    }

    setErrorMessage(null);
    setIsSaving(true);

    try {
      const device = await deviceService.create({
        customerId: customer.id,
        deviceType: form.deviceType,
        brand: form.brand,
        model: form.model,
        serialNumber: form.serialNumber.trim() || undefined,
      });

      // Lưu thiết bị vào Zustand.
      // Không chuyển sang bước issue tại đây.
      setDevice(device);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  // Phòng trường hợp store chưa có khách hàng.
  if (!customer) {
    return (
      <div>
        <ErrorMessage message="Không tìm thấy khách hàng. Vui lòng quay lại chọn khách hàng." />
        <div className={styles.actions}>
          <Button variant="secondary" onClick={() => setStep('customer')}>
            ← Quay lại
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.actions}>
        <Button variant="secondary" onClick={() => setStep('customer')}>
          ← Quay lại
        </Button>
      </div>
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
                type="button"
                className={`${styles.toggleBtn} ${mode === 'existing' ? styles.toggleActive : ''}`}
                onClick={() => setMode('existing')}
              >
                Thiết bị đã có
              </button>

              <button
                type="button"
                className={`${styles.toggleBtn} ${mode === 'new' ? styles.toggleActive : ''}`}
                onClick={() => setMode('new')}
              >
                + Thiết bị mới
              </button>
            </div>
          )}

          {mode === 'existing' && existingDevices.length > 0 ? (
            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: 8,
                flexWrap: 'wrap',
              }}
            >
              {existingDevices.map((d) => (
                <button
                  key={d.id}
                  type="button"
                  className={styles.deviceOption}
                  onClick={() => setDevice(d)}
                  aria-pressed={selectedDevice?.id === d.id}
                >
                  <strong>
                    {d.brand} {d.model}
                  </strong>{' '}
                  — {d.serialNumber ?? 'Không có Serial'}
                </button>
              ))}
            </div>
          ) : (
            <form onSubmit={handleCreateNew} className={styles.form}>
              {errorMessage && <ErrorMessage message={errorMessage} />}

              <Select
                label="Loại thiết bị"
                options={DEVICE_TYPE_OPTIONS}
                value={form.deviceType}
                onChange={(e) =>
                  setForm({
                    ...form,
                    deviceType: e.target.value as DeviceType,
                  })
                }
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
                Lưu thiết bị
              </Button>
            </form>
          )}
        </>
      )}

      {selectedDevice && (
        <div className={styles.selectedCard} role="status">
          <strong>Thiết bị đã ghi nhận</strong>
          <p>
            {selectedDevice.brand} {selectedDevice.model}
          </p>
          <p>Serial/IMEI: {selectedDevice.serialNumber || 'Không có'}</p>
          <p>Vui lòng kiểm tra thiết bị thực tế trước khi tiếp tục.</p>
        </div>
      )}
    </div>
  );
}
