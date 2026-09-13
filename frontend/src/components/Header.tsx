import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { Button } from './common';
import styles from './Header.module.css';

interface HeaderProps {
  showAuthActions?: boolean; // false cho PublicLayout khi chưa login
}

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Quản trị viên',
  Receptionist: 'Lễ tân',
  Technician: 'Kỹ thuật viên',
  Customer: 'Khách hàng',
};

export default function Header({ showAuthActions = true }: HeaderProps) {
  const { email, role, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const initial = email ? email[0].toUpperCase() : 'U';

  return (
    <header className={styles.header}>
      <div className={styles.leftArea}>
        <span className={styles.systemStatusDot} title="Hệ thống hoạt động bình thường" />
        <span className={styles.systemStatusText}>Hệ thống vận hành</span>
      </div>
      {showAuthActions && email ? (
        <div className={styles.userArea}>
          <div className={styles.profileBadge}>
            <div className={styles.avatar}>{initial}</div>
            <div className={styles.meta}>
              <span className={styles.userEmail}>{email}</span>
              <span className={`${styles.roleTag} ${role ? styles[role.toLowerCase()] : ''}`}>
                {role ? ROLE_LABELS[role] ?? role : ''}
              </span>
            </div>
          </div>
          <Button variant="secondary" size="sm" onClick={handleLogout}>
            Đăng xuất
          </Button>
        </div>
      ) : (
        showAuthActions && (
          <Button variant="primary" size="sm" onClick={() => navigate('/login')}>
            Đăng nhập
          </Button>
        )
      )}
    </header>
  );
}

