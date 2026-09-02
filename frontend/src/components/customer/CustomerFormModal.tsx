import { type FormEvent, useEffect, useState } from 'react';
import { Modal, Button, Input, ErrorMessage } from '../common';
import type { Customer } from '../../types/customer.types';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

interface CustomerFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaved: (customer: Customer) => void;
  editingCustomer?: Customer | null; // null/undefined = tạo mới
}

export default function CustomerFormModal({
  isOpen,
  onClose,
  onSaved,
  editingCustomer,
}: CustomerFormModalProps) {
  const [form, setForm] = useState({ fullName: '', phone: '', email: '', address: '' });
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  useEffect(() => {
    if (isOpen) {
      setForm({
        fullName: editingCustomer?.fullName ?? '',
        phone: editingCustomer?.phone ?? '',
        email: editingCustomer?.email ?? '',
        address: editingCustomer?.address ?? '',
      });
      setErrorMessage(null);
      setErrors([]);
    }
  }, [isOpen, editingCustomer]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSaving(true);

    try {
      const payload = {
        fullName: form.fullName,
        phone: form.phone,
        email: form.email || undefined,
        address: form.address || undefined,
      };

      const result = editingCustomer
        ? await customerService.update({ id: editingCustomer.id, ...payload })
        : await customerService.create(payload);

      onSaved(result);
      showSuccess(
        editingCustomer ? 'Đã cập nhật thông tin khách hàng.' : 'Đã tạo khách hàng mới thành công.'
      );
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
      title={editingCustomer ? 'Sửa thông tin khách hàng' : 'Tạo khách hàng mới'}
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
          label="Email"
          type="email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
        />

        <Input
          label="Địa chỉ"
          value={form.address}
          onChange={(e) => setForm({ ...form, address: e.target.value })}
        />
      </form>
    </Modal>
  );
}
