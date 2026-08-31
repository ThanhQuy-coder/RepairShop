import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { Button } from './common';
import styles from './Header.module.css';

interface HeaderProps {
  showAuthActions?: boolean; // false cho PublicLayout khi chưa login
}

export default function Header({ showAuthActions = true }: HeaderProps) {
  const { email, role, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className={styles.header}>
      <div />
      {showAuthActions && email ? (
        <div className={styles.userArea}>
          <span className={styles.userInfo}>
            {email} <em>({role})</em>
          </span>
          <Button variant="ghost" size="sm" onClick={handleLogout}>
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
