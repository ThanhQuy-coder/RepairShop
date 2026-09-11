import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import type { NavItem } from '../constants/navigation';
import styles from './Sidebar.module.css';

interface SidebarProps {
  items: NavItem[];
  title: string;
}

export default function Sidebar({ items, title }: SidebarProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      {/* Hamburger button — chỉ hiện trên màn hình nhỏ (CSS @media ẩn/hiện) */}
      <button className={styles.hamburger} onClick={() => setIsOpen(true)} aria-label="Mở menu">
        ☰
      </button>

      {/* Overlay tối phía sau khi mở sidebar trên mobile — bấm ra ngoài để đóng */}
      {isOpen && <div className={styles.overlay} onClick={() => setIsOpen(false)} />}

      <aside className={`${styles.sidebar} ${isOpen ? styles.sidebarOpen : ''}`}>
        <div className={styles.brandRow}>
          <div className={styles.brand}>{title}</div>
          <button
            className={styles.closeButton}
            onClick={() => setIsOpen(false)}
            aria-label="Đóng menu"
          >
            ×
          </button>
        </div>
        <nav className={styles.nav}>
          {items.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={() => setIsOpen(false)} // đóng sidebar sau khi chọn menu trên mobile
              className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
    </>
  );
}
