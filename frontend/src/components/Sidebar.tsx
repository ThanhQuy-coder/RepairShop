import { useEffect, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import type { NavGroup, NavItem } from '../constants/navigation';
import styles from './Sidebar.module.css';

interface SidebarProps {
  items?: NavItem[];
  groups?: NavGroup[];
  title: string;
}

export default function Sidebar({ items = [], groups = [], title }: SidebarProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const location = useLocation();

  const hasGroups = groups.length > 0;
  const activeGroup = groups.find((group) =>
    group.items.some((item) => location.pathname === item.path || location.pathname.startsWith(`${item.path}/`)),
  )?.key;

  useEffect(() => {
    if (activeGroup) setOpenGroup(activeGroup);
  }, [activeGroup]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpenGroup(null);
        setIsOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const closeMenu = () => setIsOpen(false);

  const renderItem = (item: NavItem) => (
    <NavLink
      key={item.path}
      to={item.path}
      end={item.end}
      onClick={closeMenu}
      className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
    >
      {item.label}
    </NavLink>
  );

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
          {hasGroups
            ? groups.map((group) => {
                const isActive = activeGroup === group.key;
                const isExpanded = openGroup === group.key;
                return (
                  <div className={styles.group} key={group.key}>
                    <button
                      type="button"
                      className={`${styles.groupButton} ${isActive ? styles.groupActive : ''}`}
                      aria-expanded={isExpanded}
                      aria-controls={`submenu-${group.key}`}
                      onClick={() => setOpenGroup(isExpanded ? null : group.key)}
                    >
                      <span>{group.label}</span>
                      <span className={`${styles.chevron} ${isExpanded ? styles.chevronOpen : ''}`} aria-hidden="true">⌄</span>
                    </button>
                    <div id={`submenu-${group.key}`} className={`${styles.submenu} ${isExpanded ? styles.submenuOpen : ''}`}>
                      {group.items.map(renderItem)}
                    </div>
                  </div>
                );
              })
            : items.map(renderItem)}
        </nav>
      </aside>
    </>
  );
}
