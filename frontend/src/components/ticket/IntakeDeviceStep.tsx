import { type FormEvent, useEffect, useState } from 'react';
import { Button, Input, Select, ErrorMessage, Loading } from '../common';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import type { Device, DeviceType } from '../../types/device.types';
import styles from './IntakeStep.module.css';
import { useTicketIntakeStore, type WizardStep } from '../../store/ticketIntakeStore';
import { useToastStore } from '../../store/toastStore';

const DEVICE_TYPE_OPTIONS = [
  { value: '', label: 'Chọn loại thiết bị' },
  { value: 'Tablet', label: 'Máy tính bảng' },
  { value: 'Smartwatch', label: 'Đồng hồ thông minh' },
  { value: 'Phone', label: 'Điện thoại' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Electronics', label: 'Thiết bị điện tử khác' },
];

export default function IntakeDeviceStep() {
  const customer = useTicketIntakeStore((s) => s.customer);
  const appointmentData = useTicketIntakeStore((s) => s.appointmentData);
  const selectedDevice = useTicketIntakeStore((s) => s.device);
  const setDevice = useTicketIntakeStore((s) => s.setDevice);
  const addToast = useToastStore((s) => s.addToast);
  const setStep = useTicketIntakeStore((s) => s.setStep);

  const handleChangeStep = (nextStep: WizardStep) => {
    if (appointmentData) {
      addToast(
        'error',
        'Vui lòng thoát khỏi quy trình ghi nhận để thay đổi khách hàng hoặc thông tin khách hàng.'
      );
      return;
    }
    setStep(nextStep);
  };

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
    addToast('info', 'Vui lòng kiểm tra thông tin thực tế trước khi tiếp tục.');
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
      if (form.deviceType.trim() === '' || form.brand.trim() === '' || form.model.trim() === '') {
        addToast('error', 'Vui lòng điền đầy đủ thông tin thiết bị.');
        return;
      }
      const device = await deviceService.create({
        customerId: customer.id,
        deviceType: form.deviceType,
        brand: form.brand,
        model: form.model,
        serialNumber: form.serialNumber.trim() || undefined,
      });
      addToast('success', `Đã lưu thiết bị ${device.brand} ${device.model} thành công.`);
      setDevice(device);
    } catch (err) {
      addToast('error', 'Không thể lưu thiết bị. Vui lòng thử lại.');
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
          <Button variant="secondary" onClick={() => handleChangeStep('customer')}>
            ← Quay lại
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.actions}>
        <Button variant="secondary" onClick={() => handleChangeStep('customer')}>
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
              {existingDevices.map((d) => {
                const isLocked = d.isConvertTicket === true;
                return (
                  <button
                    key={d.id}
                    type="button"
                    className={styles.deviceOption}
                    onClick={() => setDevice(d)}
                    disabled={isLocked}
                    aria-pressed={selectedDevice?.id === d.id}
                    title={isLocked ? 'Thiết bị đang có ticket chưa xử lý xong' : undefined}
                  >
                    <strong>
                      {d.brand} {d.model}
                    </strong>{' '}
                    — {d.serialNumber ?? 'Không có Serial'}
                    {isLocked && <span className={styles.badge}>Đang có ticket</span>}
                  </button>
                );
              })}
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
    </div>
  );
}
