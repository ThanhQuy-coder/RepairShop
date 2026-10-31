import { type FormEvent, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../../services/authService';
import { useAuth } from '../../hooks/useAuth';
import { extractApiError } from '../../utils/apiError';
import { isValidEmail, type FieldErrors } from '../../utils/validators';
import type { UserRole } from '../../types/auth.types';
import { Button, Input, ErrorMessage } from '../../components/common';
import styles from './AuthPage.module.css';
import { DEMO_ACCOUNTS, ENABLE_DEMO_LOGIN } from '../../configs/demoAccounts';

// Điều hướng theo role NGAY sau khi login — đúng yêu cầu API Contract Tuần 2
const ROLE_REDIRECT: Partial<Record<UserRole, string>> = {
  Admin: '/admin/dashboard',
  Receptionist: '/staff/dashboard',
  Technician: '/technician/tickets',
  Customer: '/customer/home',
};

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [apiErrorMessage, setApiErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const validate = (): boolean => {
    const errors: FieldErrors = {};
    if (!email.trim()) errors.email = 'Vui lòng nhập email.';
    else if (!isValidEmail(email)) errors.email = 'Email không đúng định dạng.';

    if (!password) errors.password = 'Vui lòng nhập mật khẩu.';

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const performLogin = async (credentials: { email: string; password: string }) => {
    setIsLoading(true);
    setApiErrorMessage(null);

    try {
      const result = await authService.login(credentials);
      const role = result.role as UserRole;

      login(result.accessToken, role, result.email, result.userId);

      const redirectPath = sessionStorage.getItem('post-login-redirect');
      sessionStorage.removeItem('post-login-redirect');

      if (redirectPath && (role === 'Customer' || role === 'Receptionist')) {
        navigate(redirectPath, { replace: true });
        return;
      }

      navigate(ROLE_REDIRECT[role] ?? '/dashboard', {
        replace: true,
      });
    } catch (err) {
      const apiError = extractApiError(err);
      setApiErrorMessage(apiError.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDemoLogin = async (account: (typeof DEMO_ACCOUNTS)[number]) => {
    if (isLoading) return;

    setEmail(account.email);
    setPassword(account.password);
    setFieldErrors({});

    await performLogin({
      email: account.email,
      password: account.password,
    });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setApiErrorMessage(null);

    if (!validate()) return;

    setIsLoading(true);
    await performLogin({ email, password });
    try {
      const result = await authService.login({ email, password });
      const role = result.role as UserRole;

      login(result.accessToken, role, result.email, result.userId);

      const redirectPath = sessionStorage.getItem('post-login-redirect');
      sessionStorage.removeItem('post-login-redirect');

      if (redirectPath && (role === 'Customer' || role === 'Receptionist')) {
        navigate(redirectPath, { replace: true });
        return;
      }

      navigate(ROLE_REDIRECT[role] ?? '/dashboard', { replace: true });
    } catch (err) {
      const apiError = extractApiError(err);
      setApiErrorMessage(apiError.message); // "Email hoặc mật khẩu không chính xác." (Backend Task 6)
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      {ENABLE_DEMO_LOGIN && (
        <div className={styles.demoSection}>
          <p className={styles.demoTitle}>Đăng nhập nhanh bằng tài khoản demo</p>

          <div className={styles.demoButtons}>
            {DEMO_ACCOUNTS.map((account) => (
              <Button
                key={account.role}
                type="button"
                className={styles.demoButton}
                isLoading={isLoading}
                onClick={() => handleDemoLogin(account)}
              >
                {account.label}
              </Button>
            ))}
          </div>
        </div>
      )}

      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <h2 className={styles.title}>Đăng nhập</h2>

        {apiErrorMessage && <ErrorMessage message={apiErrorMessage} />}

        <Input
          label="Email"
          type="email"
          name="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          errorMessage={fieldErrors.email}
          autoComplete="username"
        />

        <Input
          label="Mật khẩu"
          type="password"
          name="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          errorMessage={fieldErrors.password}
          autoComplete="current-password"
        />

        <Button type="submit" isLoading={isLoading} className={styles.submitButton}>
          Đăng nhập
        </Button>

        <p className={styles.footerText}>
          Chưa có tài khoản? <Link to="/register">Đăng ký</Link>
        </p>
      </form>
    </div>
  );
}
