import { useEffect, useState } from 'react';
import { Outlet, Link } from 'react-router-dom';
import { PUBLIC_NAV } from '../constants/navigation';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/common';
import styles from './PublicLayout.module.css';

export default function PublicLayout() {
  const { isAuthenticated } = useAuth();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const closeMenu = () => setIsMenuOpen(false);

  useEffect(() => {
    if (!isMenuOpen) return;
    const onClick = (e: MouseEvent) => {
      if (!(e.target as Element).closest(`.${styles.navbar}`)) {
        setIsMenuOpen(false);
      }
    };
    document.addEventListener('click', onClick);
    return () => document.removeEventListener('click', onClick);
  }, [isMenuOpen]);

  return (
    <div className={styles.container}>
      <header className={styles.navbar}>
        <Link to="/" className={styles.logo} onClick={closeMenu}>
          RepairShop
        </Link>

        <button
          type="button"
          className={styles.hamburger}
          onClick={() => setIsMenuOpen((prev) => !prev)}
          aria-label={isMenuOpen ? 'Đóng menu điều hướng' : 'Mở menu điều hướng'}
          aria-expanded={isMenuOpen}
          aria-controls="public-nav"
        >
          {isMenuOpen ? (
            // X icon
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          ) : (
            // Hamburger icon
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <line x1="3" y1="6" x2="21" y2="6" />
              <line x1="3" y1="12" x2="21" y2="12" />
              <line x1="3" y1="18" x2="21" y2="18" />
            </svg>
          )}
        </button>

        <nav id="public-nav" className={`${styles.nav} ${isMenuOpen ? styles.navOpen : ''}`}>
          {PUBLIC_NAV.map((item) => (
            <Link key={item.path} to={item.path} className={styles.navLink} onClick={closeMenu}>
              {item.label}
            </Link>
          ))}
        </nav>

        <div className={styles.cta}>
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
