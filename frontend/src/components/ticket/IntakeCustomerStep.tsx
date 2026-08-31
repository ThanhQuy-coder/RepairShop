import { type FormEvent, useState } from 'react';
import { Button, Input, ErrorMessage } from '../common';
import CustomerPicker from '../customer/CustomerPicker';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import type { Customer } from '../../types/customer.types';
import styles from './IntakeStep.module.css';

interface IntakeCustomerStepProps {
  onNext: (customer: Customer) => void;
}

export default function IntakeCustomerStep({ onNext }: IntakeCustomerStepProps) {
  const [mode, setMode] = useState<'existing' | 'new'>('existing');
  const [form, setForm] = useState({ fullName: '', phone: '', email: '' });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleCreateNew = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);
    try {
      const customer = await customerService.create({
        fullName: form.fullName,
        phone: form.phone,
        email: form.email || undefined,
      });
      onNext(customer);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div>
      <div className={styles.toggle}>
        <button
          className={`${styles.toggleBtn} ${mode === 'existing' ? styles.toggleActive : ''}`}
          onClick={() => setMode('existing')}
        >
          Khách hàng đã có
        </button>
        <button
          className={`${styles.toggleBtn} ${mode === 'new' ? styles.toggleActive : ''}`}
          onClick={() => setMode('new')}
        >
          + Khách hàng mới
        </button>
      </div>

      {mode === 'existing' ? (
        <div>
          <p className={styles.hint}>Tìm khách hàng theo tên hoặc số điện thoại:</p>
          <CustomerPicker onSelect={onNext} />
        </div>
      ) : (
        <form onSubmit={handleCreateNew} className={styles.form}>
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
