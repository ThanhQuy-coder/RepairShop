import { NavLink } from 'react-router-dom';
import type { NavItem } from '../constants/navigation';
import styles from './Sidebar.module.css';

interface SidebarProps {
  items: NavItem[];
  title: string;
}

export default function Sidebar({ items, title }: SidebarProps) {
  return (
    <aside className={styles.sidebar}>
      <div className={styles.brand}>{title}</div>
      <nav className={styles.nav}>
        {items.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}