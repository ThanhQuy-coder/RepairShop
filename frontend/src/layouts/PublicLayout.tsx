import { Outlet, Link } from 'react-router-dom';
import { PUBLIC_NAV } from '../constants/navigation';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/common';
import styles from './PublicLayout.module.css';

export default function PublicLayout() {
  const { isAuthenticated } = useAuth();

  return (
    <div className={styles.container}>
      <header className={styles.navbar}>
        <Link to="/" className={styles.logo}>
          RepairShop
        </Link>
        <nav className={styles.nav}>
          {PUBLIC_NAV.map((item) => (
            <Link key={item.path} to={item.path} className={styles.navLink}>
              {item.label}
            </Link>
          ))}
        </nav>
        <div>
          {isAuthenticated ? (
            <Link to="/dashboard">
              <Button size="sm">Vào hệ thống</Button>
            </Link>
          ) : (
            <Link to="/login">
              <Button size="sm">Đăng nhập</Button>
            </Link>
          )}
        </div>
      </header>

      <main className={styles.content}>
        <Outlet />
      </main>

      <footer className={styles.footer}>
        <p>© 2026 RepairShop — Hệ thống quản lý bảo hành và sửa chữa thiết bị</p>
      </footer>
    </div>
  );
}
