import { Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { NAV_BY_ROLE } from '../constants/navigation';
import Sidebar from '../components/Sidebar';
import Header from '../components/Header';
import styles from './Layout.module.css';

export default function StaffLayout() {
  const { role } = useAuth();

  // role chắc chắn là Receptionist hoặc Technician tại đây — đã được RoleGuard chặn ở tầng route (Task 5.4)
  const navItems = role ? NAV_BY_ROLE[role] : [];

  return (
    <div className={styles.container}>
      <Sidebar items={navItems} title="RepairShop — Nhân viên" />
      <div className={styles.main}>
        <Header />
        <div className={styles.content}>
          <Outlet /> {/* page con render tại đây, theo đúng cấu trúc routes/pages Tuần 2 */}
        </div>
      </div>
    </div>
  );
}
