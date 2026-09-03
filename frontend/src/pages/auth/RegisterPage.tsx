import { type FormEvent, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../../services/authService';
import { useAuth } from '../../hooks/useAuth';
import { extractApiError } from '../../utils/apiError';
import { isValidEmail, type FieldErrors } from '../../utils/validators';
import type { UserRole } from '../../types/auth.types';
import { Button, Input, ErrorMessage } from '../../components/common';
import styles from './AuthPage.module.css';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [form, setForm] = useState({ fullName: '', email: '', password: '', phone: '' });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [apiError, setApiError] = useState<{ message: string; errors: string[] } | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const validate = (): boolean => {
    const errors: FieldErrors = {};
    if (!form.fullName.trim()) errors.fullName = 'Vui lòng nhập họ tên.';
    if (!isValidEmail(form.email)) errors.email = 'Email không đúng định dạng.';
    if (form.password.length < 6) errors.password = 'Mật khẩu phải có ít nhất 6 ký tự.';
    if (!form.phone.trim())
      errors.phone = 'Vui lòng nhập số điện thoại để liên kết hồ sơ khách hàng.';
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setApiError(null);
    if (!validate()) return;

    setIsLoading(true);
    try {
      const result = await authService.register({
        fullName: form.fullName,
        email: form.email,
        password: form.password,
        phone: form.phone || undefined,
      });

      // FR-007: Customer tự đăng ký -> Backend gán role Customer mặc định (RegisterCommandHandler, Task 6)
      login(result.accessToken, result.role as UserRole, result.email);
      navigate('/customer/home', { replace: true });
    } catch (err) {
      const e2 = extractApiError(err);
      setApiError(e2);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <h2 className={styles.title}>Đăng ký tài khoản</h2>

        {apiError && <ErrorMessage message={apiError.message} errors={apiError.errors} />}

        <Input
          label="Họ và tên"
          name="fullName"
          value={form.fullName}
          onChange={(e) => setForm({ ...form, fullName: e.target.value })}
          errorMessage={fieldErrors.fullName}
        />

        <Input
          label="Email"
          type="email"
          name="email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          errorMessage={fieldErrors.email}
        />

        <Input
          label="Số điện thoại"
          required
          name="phone"
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
          errorMessage={fieldErrors.phone}
        />

        <Input
          label="Mật khẩu"
          type="password"
          name="password"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          errorMessage={fieldErrors.password}
        />

        <Button type="submit" isLoading={isLoading} className={styles.submitButton}>
          Đăng ký
        </Button>

        <p className={styles.footerText}>
          Đã có tài khoản? <Link to="/login">Đăng nhập</Link>
        </p>
      </form>
    </div>
  );
}
