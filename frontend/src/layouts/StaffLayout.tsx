import { Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { NAV_BY_ROLE } from '../constants/navigation';
import AdminLayout from './AdminLayout';
import Sidebar from '../components/Sidebar';
import Header from '../components/Header';
import styles from './Layout.module.css';

export default function StaffLayout() {
  const { role } = useAuth();

  if (role === 'Admin') {
    return <AdminLayout />;
  }

  const navItems = role ? NAV_BY_ROLE[role] : [];

  return (
    <div className={styles.container}>
      <Sidebar items={navItems} title="Fixora — Nhân viên" />
      <div className={styles.main}>
        <Header />
        <div className={styles.content}>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
