import { type FormEvent, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Button, ErrorMessage, Input, Loading, Select } from '../../components/common';
import {
  appointmentService,
  type AvailableSlot,
  type AppointmentResponse,
} from '../../services/appointmentService';
import { extractApiError } from '../../utils/apiError';
import styles from './BookingPage.module.css';
import { DEVICE_TYPE_OPTIONS, type DeviceType } from '../../types/device.types';
import { useDeviceCatalogCascade } from '../../hooks/useDeviceCatalogCascade';

const today = new Date().toISOString().slice(0, 10);

const BOOKING_DEVICE_TYPE_OPTIONS = [
  { value: '', label: '-- Chọn loại thiết bị --' },
  ...DEVICE_TYPE_OPTIONS,
];

export default function BookingPage() {
  const [searchParams] = useSearchParams();
  const initialDeviceType = searchParams.get('deviceType') ?? '';

  const [date, setDate] = useState(today);
  const [slots, setSlots] = useState<AvailableSlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [deviceType, setDeviceType] = useState(initialDeviceType);
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [serial, setSerial] = useState('');
  const [issueDescription, setIssueDescription] = useState('');
  const [appointmentType, setAppointmentType] = useState<'Repair' | 'Pickup'>('Repair');
  const [result, setResult] = useState<AppointmentResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoadingSlots, setIsLoadingSlots] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { brands, models, loadingBrands, loadingModels } = useDeviceCatalogCascade(
    deviceType,
    brand
  );

  useEffect(() => {
    appointmentService
      .getAvailableSlots(date)
      .then(setSlots)
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoadingSlots(false));
  }, [date]);

  const handleDeviceTypeChange = (nextType: string) => {
    setDeviceType(nextType as DeviceType);
    setBrand('');
    setModel('');
  };

  const handleBrandChange = (nextBrand: string) => {
    setBrand(nextBrand);
    setModel('');
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedSlot) {
      setErrorMessage('Vui lòng chọn một khung giờ còn trống.');
      return;
    }
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      const created = await appointmentService.create({
        fullName,
        phone,
        appointmentDate: date,
        timeSlotId: selectedSlot,
        customerId: null,
        deviceInfo: { deviceType, brand, model, issueDescription, serial },
        type: appointmentType,
      });
      if (created.lookupToken) {
        try {
          localStorage.setItem(
            'appointment-lookup-token',
            JSON.stringify({
              token: created.lookupToken,
              expiresAt: Date.now() + 7 * 24 * 60 * 60 * 1000,
            })
          );
        } catch {
          /* localStorage is optional convenience */
        }
      }
      setResult(created);
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (result)
    return (
      <main className={styles.page}>
        <section className={styles.success}>
          <span className={styles.successIcon}>✓</span>
          <span className={styles.kicker}>FIXORA • ĐẶT LỊCH THÀNH CÔNG</span>
          <h1>Đã ghi nhận lịch hẹn của bạn.</h1>
          <p>Vui lòng lưu mã tra cứu để kiểm tra hoặc quản lý lịch hẹn trên hệ thống Fixora.</p>
          <div className={styles.confirmation}>
            <span>Mã tra cứu lịch hẹn</span>
            <strong>{result.lookupToken}</strong>
            <span>Ngày hẹn</span>
            <strong>{new Date(result.appointmentDate).toLocaleDateString('vi-VN')}</strong>
            <span>Trạng thái</span>
            <strong>{result.status}</strong>
          </div>
          <div className={styles.actions}>
            <Link to="/">
              <Button>Về trang chủ Fixora</Button>
            </Link>
            <Link to="/appointments/lookup" className={styles.secondary}>
              Tra cứu lịch hẹn
            </Link>
          </div>
        </section>
      </main>
    );

  const brandOptions = [
    {
      value: '',
      label: !deviceType
        ? '-- Chọn loại thiết bị trước --'
        : loadingBrands
          ? 'Đang tải hãng sản xuất...'
          : '-- Chọn hãng sản xuất --',
    },
    ...brands.map((b) => ({ value: b.name, label: b.name })),
  ];

  const modelOptions = [
    {
      value: '',
      label: !brand
        ? '-- Chọn hãng sản xuất trước --'
        : loadingModels
          ? 'Đang tải dòng máy...'
          : '-- Chọn dòng máy (Model) --',
    },
    ...models.map((m) => ({
      value: m.modelName,
      label: m.modelNumber ? `${m.modelName} (${m.modelNumber})` : m.modelName,
    })),
  ];

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <span className={styles.kicker}>FIXORA • ĐẶT LỊCH SỬA CHỮA</span>
        <h1>Chọn thời gian thuận tiện cho bạn.</h1>
        <p>Đặt lịch trước giúp đội ngũ kỹ thuật viên Fixora chuẩn bị sẵn linh kiện và ưu tiên tiếp nhận thiết bị của bạn.</p>
        {searchParams.get('serviceId') && (
          <small>Dịch vụ đã chọn sẽ được xác nhận khi tiếp nhận thiết bị tại cửa hàng.</small>
        )}
        <Link to="/appointments/lookup" className={styles.secondary}>
          Đã có lịch hẹn? Tra cứu ngay →
        </Link>
      </header>
      <form onSubmit={submit} className={styles.form}>
        {errorMessage && <ErrorMessage message={errorMessage} />}
        <div className={styles.formSection}>
          <h2>1. Ngày và khung giờ</h2>
          <label>
            Ngày hẹn
            <input
              type="date"
              min={today}
              value={date}
              onChange={(event) => {
                setDate(event.target.value);
                setSelectedSlot('');
                setIsLoadingSlots(true);
              }}
              required
            />
          </label>
          {isLoadingSlots ? (
            <Loading />
          ) : slots.length === 0 ? (
            <p className={styles.empty}>Ngày này chưa có khung giờ.</p>
          ) : (
            <div className={styles.slots}>
              {slots.map((slot) => (
                <button
                  type="button"
                  key={slot.timeSlotId}
                  disabled={slot.isFull}
                  className={selectedSlot === slot.timeSlotId ? styles.selectedSlot : styles.slot}
                  onClick={() => setSelectedSlot(slot.timeSlotId)}
                >
                  {slot.slotStart} – {slot.slotEnd}
                  <small>{slot.isFull ? 'Đã đầy' : `Còn ${slot.available} chỗ`}</small>
                </button>
              ))}
            </div>
          )}
        </div>
        <div className={styles.formSection}>
          <h2>2. Thông tin khách hàng & thiết bị</h2>
          <label>
            <Select
              label="Loại cuộc hẹn"
              options={[
                { value: 'Repair', label: 'Sửa chữa thiết bị' },
                { value: 'Pickup', label: 'Nhận thiết bị' },
                { value: 'WarrantyRevisit', label: 'Bảo hành lại' },
              ]}
              value={appointmentType}
              onChange={(event) => setAppointmentType(event.target.value as 'Repair' | 'Pickup')}
            />
          </label>
          <div className={styles.twoColumns}>
            <Input
              label="Họ và tên (Bắt buộc)"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              required
            />
            <Input
              label="Số điện thoại (Bắt buộc)"
              type="tel"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              required
            />
          </div>
          <div className={styles.twoColumns}>
            <Select
              label="Loại thiết bị"
              options={BOOKING_DEVICE_TYPE_OPTIONS}
              value={deviceType}
              onChange={(event) => handleDeviceTypeChange(event.target.value)}
            />
            <Select
              label="Hãng sản xuất"
              options={brandOptions}
              value={brand}
              disabled={!deviceType || loadingBrands}
              onChange={(event) => handleBrandChange(event.target.value)}
            />
            <Select
              label="Dòng máy (Model)"
              options={modelOptions}
              value={model}
              disabled={!brand || loadingModels}
              onChange={(event) => setModel(event.target.value)}
            />
            <Input
              label="Số Serial / IMEI (Nếu có)"
              value={serial}
              onChange={(event) => setSerial(event.target.value)}
              placeholder="Ví dụ: C02X..."
            />
          </div>
          <label>
            Mô tả tình trạng
            <textarea
              value={issueDescription}
              onChange={(event) => setIssueDescription(event.target.value)}
              rows={4}
              placeholder="Mô tả ngắn vấn đề thiết bị đang gặp phải (ví dụ: Pin tụt nhanh, màn hình bị sọc, máy nóng...)"
            />
          </label>
        </div>
        <Button type="submit" size="lg" isLoading={isSubmitting}>
          Xác nhận đặt lịch tại Fixora
        </Button>
      </form>
    </main>
  );
}
