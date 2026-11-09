import { type FormEvent, type ReactNode, useState } from 'react';
import { Button, ErrorMessage, Input, Select } from '../../components/common';
import {
  ArrowRight,
  CalendarCheck,
  CheckCircle2,
  Clock,
  Cpu,
  Laptop,
  ShieldCheck,
  Smartphone,
  Sparkles,
  Wrench,
} from '../../components/common/FixoraIcons';
import { useDeviceCatalogCascade } from '../../hooks/useDeviceCatalogCascade';
import { consultationService } from '../../services/consultationService';
import type {
  ConsultationResponse,
  ConsultationSlot,
} from '../../types/consultation.types';
import { DEVICE_TYPE_OPTIONS, type DeviceType } from '../../types/device.types';
import { extractApiError } from '../../utils/apiError';
import styles from './ConsultationPage.module.css';

const CONFIDENCE_LABEL: Record<string, string> = {
  HIGH: 'Độ tin cậy cao',
  MEDIUM: 'Trung bình',
  LOW: 'Thấp',
};

const STATUS_META: Record<
  string,
  { label: string; tone: 'success' | 'warning' | 'muted' | 'danger' }
> = {
  SUCCESS: { label: 'Đã tư vấn', tone: 'success' },
  PARTIAL: { label: 'Tư vấn một phần', tone: 'warning' },
  NO_MATCH: { label: 'Chưa đủ dữ liệu', tone: 'muted' },
  OUT_OF_SCOPE: { label: 'Ngoài phạm vi', tone: 'muted' },
  ERROR: { label: 'Tạm thời gián đoạn', tone: 'danger' },
};

const money = new Intl.NumberFormat('vi-VN');

function formatMoney(value: number): string {
  return `${money.format(value)} ₫`;
}

function formatDate(value: string | null | undefined): string {
  if (!value) return '';
  const [year, month, day] = value.split('-');
  return year && month && day ? `${day}/${month}/${year}` : value;
}

function deviceIcon(deviceType: DeviceType): ReactNode {
  if (deviceType === 'Laptop') return <Laptop size={18} />;
  if (deviceType === 'Electronics') return <Cpu size={18} />;
  return <Smartphone size={18} />;
}

export default function ConsultationPage() {
  const [deviceType, setDeviceType] = useState<DeviceType>('Phone');
  const [brandId, setBrandId] = useState('');
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [issueDescription, setIssueDescription] = useState('');
  const [requestedDate, setRequestedDate] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');

  const [result, setResult] = useState<ConsultationResponse | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<ConsultationSlot | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isBooking, setIsBooking] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [bookingMessage, setBookingMessage] = useState<string | null>(null);

  const { brands, loadingBrands } = useDeviceCatalogCascade(deviceType, brandId);

  const today = new Date().toISOString().slice(0, 10);

  const handleDeviceTypeChange = (nextType: DeviceType) => {
    setDeviceType(nextType);
    setBrandId('');
    setBrand('');
    setModel('');
  };

  const handleBrandSelect = (nextBrandId: string) => {
    setBrandId(nextBrandId);
    const found = brands.find((item) => item.id === nextBrandId);
    setBrand(found ? found.name : '');
  };

  const handleConsult = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    setBookingMessage(null);

    if (!brand.trim() || !model.trim()) {
      setErrorMessage('Vui lòng chọn hoặc nhập hãng và dòng máy.');
      return;
    }
    if (issueDescription.trim().length < 5) {
      setErrorMessage('Vui lòng mô tả tình trạng máy rõ hơn (ít nhất 5 ký tự).');
      return;
    }

    setIsLoading(true);
    try {
      const response = await consultationService.consult({
        deviceType,
        brand: brand.trim(),
        model: model.trim(),
        issueDescription: issueDescription.trim(),
        requestedDate: requestedDate || undefined,
        allowQuotation: true,
        allowAppointment: true,
        fullName: fullName.trim() || undefined,
        phone: phone.trim() || undefined,
      });
      setResult(response);
      setSelectedSlot(null);
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
      setResult(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleBook = async () => {
    if (!selectedSlot) {
      setErrorMessage('Vui lòng chọn một khung giờ còn trống.');
      return;
    }
    if (!fullName.trim() || !phone.trim()) {
      setErrorMessage('Vui lòng nhập họ tên và số điện thoại để đặt lịch.');
      return;
    }

    setErrorMessage(null);
    setIsBooking(true);
    try {
      const appointment = await consultationService.book({
        fullName: fullName.trim(),
        phone: phone.trim(),
        deviceInfo: {
          deviceType,
          brand: brand.trim(),
          model: model.trim(),
          issueDescription: issueDescription.trim(),
        },
        appointmentDate: selectedSlot.appointmentDate,
        timeSlotId: selectedSlot.timeSlotId,
        type: 'Repair',
      });
      setBookingMessage(
        `Đã ghi nhận lịch hẹn ngày ${formatDate(appointment.appointmentDate)} ` +
          `(trạng thái ${appointment.status}).` +
          (appointment.lookupToken
            ? ` Mã tra cứu của bạn: ${appointment.lookupToken}.`
            : ''),
      );
      setSelectedSlot(null);
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsBooking(false);
    }
  };

  const diagnosis = result?.diagnosis;
  const quotation = result?.quotation;
  const appointment = result?.appointment;
  const slots = appointment?.suggestedSlots ?? [];
  const statusMeta = result ? (STATUS_META[result.status] ?? STATUS_META.ERROR) : null;

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <span className={styles.eyebrow}>
          <Sparkles size={15} /> Trợ lý AI Fixora
        </span>
        <h1 className={styles.heroTitle}>
          Chẩn đoán, báo giá và đặt lịch trong một lần tư vấn
        </h1>
        <p className={styles.heroText}>
          Mô tả triệu chứng thiết bị để nhận gợi ý dịch vụ, chi phí tham khảo và khung giờ còn
          trống. Mọi thông tin đều được đối chiếu với dữ liệu thực tế của cửa hàng.
        </p>
        <div className={styles.heroChips}>
          <span>
            <Clock size={15} /> Phản hồi trong vài giây
          </span>
          <span>
            <ShieldCheck size={15} /> Không tự chốt giá
          </span>
          <span>
            <CalendarCheck size={15} /> Đặt lịch sau khi xác nhận
          </span>
        </div>
      </section>

      <div className={styles.layout}>
        <form className={`${styles.card} ${styles.formCard}`} onSubmit={handleConsult}>
          <header className={styles.cardHeader}>
            <span className={styles.stepBadge}>1</span>
            <div>
              <h2 className={styles.cardTitle}>Thông tin thiết bị</h2>
              <p className={styles.cardSub}>Chọn đúng máy để trợ lý tra cứu dịch vụ phù hợp.</p>
            </div>
          </header>

          <div className={styles.field}>
            <span className={styles.label}>Loại thiết bị</span>
            <div className={styles.deviceSwitch}>
              {DEVICE_TYPE_OPTIONS.map((option) => (
                <button
                  type="button"
                  key={option.value}
                  className={
                    deviceType === option.value
                      ? `${styles.deviceOption} ${styles.deviceOptionActive}`
                      : styles.deviceOption
                  }
                  onClick={() => handleDeviceTypeChange(option.value as DeviceType)}
                >
                  {deviceIcon(option.value as DeviceType)}
                  <span>{option.label}</span>
                </button>
              ))}
            </div>
          </div>

          <div className={styles.grid2}>
            <Select
              label="Hãng sản xuất"
              value={brandId}
              disabled={loadingBrands}
              onChange={(event) => handleBrandSelect(event.target.value)}
              options={[
                {
                  value: '',
                  label: loadingBrands ? 'Đang tải hãng...' : '-- Chọn hãng --',
                },
                ...brands.map((item) => ({ value: item.id, label: item.name })),
              ]}
            />
            <Input
              label="Hoặc nhập hãng"
              value={brand}
              onChange={(event) => setBrand(event.target.value)}
              placeholder="VD: Apple, Samsung, Dell..."
            />
          </div>

          <div className={styles.grid2}>
            <Input
              label="Dòng máy"
              value={model}
              onChange={(event) => setModel(event.target.value)}
              placeholder="VD: iPhone 13, Inspiron 15..."
            />
            <Input
              label="Ngày muốn đến (không bắt buộc)"
              type="date"
              min={today}
              value={requestedDate}
              onChange={(event) => setRequestedDate(event.target.value)}
            />
          </div>

          <div className={styles.divider} />

          <header className={styles.cardHeader}>
            <span className={styles.stepBadge}>2</span>
            <div>
              <h2 className={styles.cardTitle}>Mô tả tình trạng</h2>
              <p className={styles.cardSub}>
                Càng cụ thể càng tốt: hiện tượng, thời điểm, mức độ thường gặp.
              </p>
            </div>
          </header>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="issue">
              Triệu chứng / yêu cầu
            </label>
            <textarea
              id="issue"
              className={styles.textarea}
              rows={5}
              maxLength={500}
              value={issueDescription}
              onChange={(event) => setIssueDescription(event.target.value)}
              placeholder="VD: Pin tụt nhanh, máy nóng khi sạc. Muốn báo giá và đặt lịch mai 3h chiều..."
            />
            <span className={styles.counter}>{issueDescription.length}/500</span>
          </div>

          {errorMessage && <ErrorMessage message={errorMessage} />}

          <div className={styles.actions}>
            <Button type="submit" isLoading={isLoading} className={styles.submit}>
              <Sparkles size={16} /> Nhận tư vấn & gợi ý khung giờ
            </Button>
            <p className={styles.hint}>
              Không cần đăng nhập. Giá và lịch hẹn chỉ được xác nhận bởi cửa hàng.
            </p>
          </div>
        </form>

        <div className={styles.results}>
          {!result && !isLoading && (
            <div className={`${styles.card} ${styles.placeholder}`}>
              <span className={styles.placeholderIcon}>
                <Wrench size={22} />
              </span>
              <h2 className={styles.cardTitle}>Trợ lý sẽ làm gì?</h2>
              <ul className={styles.placeholderList}>
                <li>
                  <CheckCircle2 size={16} />
                  <span>Phân tích triệu chứng và nêu nguyên nhân có thể.</span>
                </li>
                <li>
                  <CheckCircle2 size={16} />
                  <span>Gợi ý dịch vụ, linh kiện phù hợp từ danh mục cửa hàng.</span>
                </li>
                <li>
                  <CheckCircle2 size={16} />
                  <span>Tính báo giá tham khảo theo bảng giá hiện hành.</span>
                </li>
                <li>
                  <CheckCircle2 size={16} />
                  <span>Đề xuất khung giờ còn trống và chuẩn bị thông tin đặt lịch.</span>
                </li>
              </ul>
              <p className={styles.placeholderNote}>
                Kết quả là tư vấn tham khảo — kỹ thuật viên sẽ kiểm tra trực tiếp trước khi
                chốt phương án.
              </p>
            </div>
          )}

          {isLoading && (
            <div className={`${styles.card} ${styles.loadingCard}`} aria-live="polite">
              <span className={styles.spinner} />
              <div>
                <h2 className={styles.cardTitle}>Đang phân tích yêu cầu…</h2>
                <p className={styles.cardSub}>
                  Hệ thống đang tra cứu dịch vụ, bảng giá và khung giờ còn trống.
                </p>
              </div>
              <div className={styles.skeleton}>
                <span />
                <span />
                <span />
              </div>
            </div>
          )}

          {result && statusMeta && (
            <div className={styles.resultStack} aria-live="polite">
              <div className={`${styles.card} ${styles.statusCard}`}>
                <div className={styles.statusTop}>
                  <span className={`${styles.statusBadge} ${styles[statusMeta.tone]}`}>
                    {statusMeta.label}
                  </span>
                  {result.executedAgents.length > 0 && (
                    <div className={styles.agentChips}>
                      {result.executedAgents.map((agent) => (
                        <span key={agent}>{agent}</span>
                      ))}
                    </div>
                  )}
                </div>
                <p className={styles.message}>{result.message}</p>
                {!result.aiAvailable && (
                  <p className={styles.inlineWarning}>
                    Trợ lý AI đang tạm gián đoạn. Bạn vẫn có thể đặt lịch trực tiếp hoặc thử lại
                    sau ít phút.
                  </p>
                )}
              </div>

              {diagnosis && diagnosis.possibleCauses.length > 0 && (
                <section className={styles.card}>
                  <header className={styles.sectionHead}>
                    <span className={styles.sectionIcon}>
                      <Wrench size={16} />
                    </span>
                    <h2 className={styles.cardTitle}>Nguyên nhân có thể</h2>
                  </header>
                  <ul className={styles.list}>
                    {diagnosis.possibleCauses.map((cause) => (
                      <li key={cause.name} className={styles.listItem}>
                        <span className={styles.itemName}>{cause.name}</span>
                        <span className={styles.confidence}>
                          {CONFIDENCE_LABEL[cause.confidence] ?? cause.confidence}
                        </span>
                      </li>
                    ))}
                  </ul>
                  <p className={styles.sectionNote}>
                    Đây là giả thuyết từ mô tả, không phải kết luận kỹ thuật cuối cùng.
                  </p>
                </section>
              )}

              {diagnosis &&
                (diagnosis.suggestedServices.length > 0 ||
                  diagnosis.suggestedParts.length > 0) && (
                  <section className={styles.card}>
                    <header className={styles.sectionHead}>
                      <span className={styles.sectionIcon}>
                        <Cpu size={16} />
                      </span>
                      <h2 className={styles.cardTitle}>Dịch vụ & linh kiện gợi ý</h2>
                    </header>
                    <ul className={styles.list}>
                      {diagnosis.suggestedServices.map((service) => (
                        <li key={service.serviceId} className={styles.listItem}>
                          <span className={styles.itemName}>{service.serviceName}</span>
                          <span className={styles.tag}>Dịch vụ</span>
                        </li>
                      ))}
                      {diagnosis.suggestedParts.map((part) => (
                        <li key={part.partId} className={styles.listItem}>
                          <span className={styles.itemName}>{part.partName}</span>
                          <span className={styles.tagMuted}>Linh kiện</span>
                        </li>
                      ))}
                    </ul>
                  </section>
                )}

              {quotation && quotation.items.length > 0 && (
                <section className={styles.card}>
                  <header className={styles.sectionHead}>
                    <span className={styles.sectionIcon}>
                      <CheckCircle2 size={16} />
                    </span>
                    <h2 className={styles.cardTitle}>Báo giá tham khảo</h2>
                  </header>
                  <div className={styles.tableWrap}>
                    <table className={styles.table}>
                      <thead>
                        <tr>
                          <th>Hạng mục</th>
                          <th>Loại</th>
                          <th className={styles.numberCell}>Đơn giá</th>
                          <th className={styles.numberCell}>Thành tiền</th>
                        </tr>
                      </thead>
                      <tbody>
                        {quotation.items.map((item) => (
                          <tr key={`${item.itemType}-${item.itemId}`}>
                            <td>{item.name}</td>
                            <td>
                              <span
                                className={
                                  item.itemType === 'SERVICE'
                                    ? styles.tag
                                    : styles.tagMuted
                                }
                              >
                                {item.itemType === 'SERVICE' ? 'Dịch vụ' : 'Linh kiện'}
                              </span>
                            </td>
                            <td className={styles.numberCell}>{formatMoney(item.unitPrice)}</td>
                            <td className={styles.numberCell}>{formatMoney(item.lineTotal)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className={styles.totalRow}>
                    <span>Tổng tham khảo</span>
                    <strong>{formatMoney(quotation.estimatedTotal)}</strong>
                  </div>
                  {quotation.assumptions.map((note) => (
                    <p key={note} className={styles.sectionNote}>
                      • {note}
                    </p>
                  ))}
                </section>
              )}

              {appointment && slots.length > 0 && (
                <section className={`${styles.card} ${styles.bookingCard}`}>
                  <header className={styles.sectionHead}>
                    <span className={styles.sectionIcon}>
                      <CalendarCheck size={16} />
                    </span>
                    <h2 className={styles.cardTitle}>Chọn khung giờ & xác nhận</h2>
                  </header>
                  <p className={styles.cardSub}>
                    Lịch hẹn được giữ ở trạng thái chờ nhân viên xác nhận. Bạn sẽ nhận mã tra cứu
                    sau khi đặt.
                  </p>

                  <div className={styles.slotGrid}>
                    {slots.map((slot) => (
                      <label
                        key={slot.timeSlotId}
                        className={
                          selectedSlot?.timeSlotId === slot.timeSlotId
                            ? `${styles.slotCard} ${styles.slotCardActive}`
                            : styles.slotCard
                        }
                      >
                        <input
                          type="radio"
                          name="slot"
                          checked={selectedSlot?.timeSlotId === slot.timeSlotId}
                          onChange={() => setSelectedSlot(slot)}
                        />
                        <span className={styles.slotTime}>
                          {slot.slotStart}–{slot.slotEnd}
                        </span>
                        <span className={styles.slotDate}>
                          {formatDate(slot.appointmentDate)}
                        </span>
                        <span className={styles.slotSeats}>Còn {slot.available} chỗ</span>
                      </label>
                    ))}
                  </div>

                  <div className={styles.bookingGrid}>
                    <Input
                      label="Họ tên"
                      value={fullName}
                      onChange={(event) => setFullName(event.target.value)}
                      placeholder="Nguyễn Văn A"
                    />
                    <Input
                      label="Số điện thoại"
                      value={phone}
                      onChange={(event) => setPhone(event.target.value)}
                      placeholder="0901 234 567"
                      inputMode="tel"
                    />
                  </div>

                  {appointment.missingInformation.length > 0 && (
                    <p className={styles.sectionNote}>
                      Còn thiếu: {appointment.missingInformation.join(', ')}
                    </p>
                  )}

                  <div className={styles.actions}>
                    <Button
                      type="button"
                      onClick={handleBook}
                      isLoading={isBooking}
                      className={styles.submit}
                    >
                      <CalendarCheck size={16} /> Xác nhận đặt lịch
                    </Button>
                  </div>
                </section>
              )}

              {bookingMessage && (
                <div className={styles.successCard}>
                  <CheckCircle2 size={18} />
                  <span>{bookingMessage}</span>
                </div>
              )}

              {result.status !== 'SUCCESS' && slots.length === 0 && (
                <div className={`${styles.card} ${styles.noticeCard}`}>
                  Chưa có khung giờ phù hợp để đặt lịch. Bạn có thể thử ngày khác hoặc liên hệ
                  cửa hàng để được hỗ trợ.
                </div>
              )}

              <p className={styles.disclaimer}>{result.disclaimer}</p>
            </div>
          )}
        </div>
      </div>

      <a className={styles.backLink} href="/services">
        Xem danh mục dịch vụ <ArrowRight size={14} />
      </a>
    </div>
  );
}
