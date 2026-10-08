import { type FormEvent, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Button, ErrorMessage, Input, Loading } from '../../components/common';
import { appointmentService, type AvailableSlot, type AppointmentResponse } from '../../services/appointmentService';
import { extractApiError } from '../../utils/apiError';
import styles from './BookingPage.module.css';

const today = new Date().toISOString().slice(0, 10);

export default function BookingPage() {
  const [searchParams] = useSearchParams();
  const [date, setDate] = useState(today);
  const [slots, setSlots] = useState<AvailableSlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [deviceType, setDeviceType] = useState('');
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [issueDescription, setIssueDescription] = useState('');
  const [result, setResult] = useState<AppointmentResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoadingSlots, setIsLoadingSlots] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    appointmentService.getAvailableSlots(date).then(setSlots).catch((error) => setErrorMessage(extractApiError(error).message)).finally(() => setIsLoadingSlots(false));
  }, [date]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedSlot) {
      setErrorMessage('Vui lòng chọn một khung giờ còn trống.');
      return;
    }
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      setResult(await appointmentService.create({
        fullName, phone, appointmentDate: date, timeSlotId: selectedSlot,
        customerId: null, deviceInfo: { deviceType, brand, model, issueDescription },
      }));
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (result) return <main className={styles.page}><section className={styles.success}><span className={styles.successIcon}>✓</span><span className={styles.kicker}>ĐẶT LỊCH THÀNH CÔNG</span><h1>Đã ghi nhận lịch hẹn của bạn.</h1><p>Vui lòng lưu lại mã lịch hẹn để được hỗ trợ nhanh hơn khi đến cửa hàng.</p><div className={styles.confirmation}><span>Mã lịch hẹn</span><strong>{result.id}</strong><span>Ngày hẹn</span><strong>{new Date(result.appointmentDate).toLocaleDateString('vi-VN')}</strong><span>Trạng thái</span><strong>{result.status}</strong></div><div className={styles.actions}><Link to="/"><Button>Về trang chủ</Button></Link><Link to="/track" className={styles.secondary}>Tra cứu phiếu sửa chữa</Link></div></section></main>;

  return <main className={styles.page}><header className={styles.header}><span className={styles.kicker}>ĐẶT LỊCH SỬA CHỮA</span><h1>Chọn thời gian thuận tiện cho bạn.</h1><p>Đặt lịch trước giúp đội ngũ chuẩn bị tốt hơn cho thiết bị của bạn.</p>{searchParams.get('serviceId') && <small>Dịch vụ đã chọn sẽ được xác nhận khi tiếp nhận thiết bị.</small>}</header><form onSubmit={submit} className={styles.form}>{errorMessage && <ErrorMessage message={errorMessage} />}<div className={styles.formSection}><h2>1. Ngày và khung giờ</h2><label>Ngày hẹn<input type="date" min={today} value={date} onChange={(event) => { setDate(event.target.value); setSelectedSlot(''); setIsLoadingSlots(true); }} required /></label>{isLoadingSlots ? <Loading /> : slots.length === 0 ? <p className={styles.empty}>Ngày này chưa có khung giờ.</p> : <div className={styles.slots}>{slots.map((slot) => <button type="button" key={slot.timeSlotId} disabled={slot.isFull} className={selectedSlot === slot.timeSlotId ? styles.selectedSlot : styles.slot} onClick={() => setSelectedSlot(slot.timeSlotId)}>{slot.slotStart} – {slot.slotEnd}<small>{slot.isFull ? 'Đã đầy' : `Còn ${slot.available} chỗ`}</small></button>)}</div>}</div><div className={styles.formSection}><h2>2. Thông tin liên hệ</h2><div className={styles.twoColumns}><Input label="Họ và tên" value={fullName} onChange={(event) => setFullName(event.target.value)} required /><Input label="Số điện thoại" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} required /></div><div className={styles.twoColumns}><Input label="Loại thiết bị" value={deviceType} onChange={(event) => setDeviceType(event.target.value)} /><Input label="Hãng" value={brand} onChange={(event) => setBrand(event.target.value)} /><Input label="Model" value={model} onChange={(event) => setModel(event.target.value)} /></div><label>Mô tả tình trạng<textarea value={issueDescription} onChange={(event) => setIssueDescription(event.target.value)} rows={4} placeholder="Mô tả ngắn vấn đề thiết bị đang gặp phải" /></label></div><Button type="submit" size="lg" isLoading={isSubmitting}>Xác nhận đặt lịch</Button></form></main>;
}
