import { type FormEvent, useEffect, useState } from 'react';
import { Button, Input, ErrorMessage } from '../common';
import CustomerPicker from '../customer/CustomerPicker';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import { useTicketIntakeStore } from '../../store/ticketIntakeStore';
import styles from './IntakeStep.module.css';
import { useNavigate } from 'react-router-dom';

export default function IntakeCustomerStep() {
  const appointmentData = useTicketIntakeStore((s) => s.appointmentData);
  const setCustomer = useTicketIntakeStore((s) => s.setCustomer);
  const navigate = useNavigate();

  const [mode, setMode] = useState<'existing' | 'new'>('existing');
  const [isResolving, setIsResolving] = useState(false);

  useEffect(() => {
    if (!appointmentData) return;

    let cancelled = false;

    const resolveCustomer = async () => {
      setIsResolving(true);

      try {
        if (appointmentData.customerId) {
          const customer = await customerService.getById(appointmentData.customerId);

          if (!cancelled) setCustomer(customer);
        } else {
          const found = await customerService.findByPhone(appointmentData.phone);

          if (cancelled) return;

          if (found) setCustomer(found);
          else setMode('new');
        }
      } catch {
        if (!cancelled) setMode('new');
      } finally {
        if (!cancelled) setIsResolving(false);
      }
    };

    void resolveCustomer();

    return () => {
      cancelled = true;
    };
  }, [appointmentData, setCustomer]);

  useEffect(() => {
    if (appointmentData) {
      setForm({
        fullName: appointmentData.fullName ?? '',
        phone: appointmentData.phone ?? '',
        email: '',
      });
    }
  }, [appointmentData]);

  const [form, setForm] = useState({
    fullName: appointmentData?.fullName ?? '',
    phone: appointmentData?.phone ?? '',
    email: '',
  });

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleCreateNew = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);

    try {
      const customer = await customerService.create({
        fullName: form.fullName.trim(),
        phone: form.phone.trim(),
        email: form.email.trim() || undefined,
      });

      setCustomer(customer);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  if (isResolving) {
    return (
      <div className={styles.loadingWrapper}>
        <p>Đang kiểm tra thông tin từ lịch hẹn...</p>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.actions}>
        <Button variant="secondary" onClick={() => navigate(-1)}>
          ← Quay lại
        </Button>
      </div>
      <div className={styles.toggle}>
        <button
          type="button"
          className={`${styles.toggleBtn} ${mode === 'existing' ? styles.toggleActive : ''}`}
          onClick={() => setMode('existing')}
        >
          Khách hàng đã có
        </button>

        <button
          type="button"
          className={`${styles.toggleBtn} ${mode === 'new' ? styles.toggleActive : ''}`}
          onClick={() => setMode('new')}
        >
          + Khách hàng mới
        </button>
      </div>

      {mode === 'existing' ? (
        <div>
          <p className={styles.hint}>Tìm khách hàng theo tên hoặc số điện thoại:</p>
          <CustomerPicker onSelect={setCustomer} />
        </div>
      ) : (
        <form onSubmit={handleCreateNew} className={styles.form}>
          {appointmentData && (
            <p className={styles.hint}>
              Thông tin được điền từ lịch hẹn. Vui lòng kiểm tra trước khi tiếp tục.
            </p>
          )}

          {errorMessage && <ErrorMessage message={errorMessage} />}

          <Input
            label="Họ và tên"
            required
            value={form.fullName}
            onChange={(e) => setForm({ ...form, fullName: e.target.value })}
          />

          <Input
            label="Số điện thoại"
            required
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
          />

          <Input
            label="Email (tùy chọn)"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
          />

          <Button type="submit" isLoading={isSaving}>
            Tạo khách hàng & Tiếp tục
          </Button>
        </form>
      )}
    </div>
  );
}
