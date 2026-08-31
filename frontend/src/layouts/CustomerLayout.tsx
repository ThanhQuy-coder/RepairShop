import { Outlet } from 'react-router-dom';
import { NAV_BY_ROLE } from '../constants/navigation';
import Sidebar from '../components/Sidebar';
import Header from '../components/Header';
import styles from './Layout.module.css';

export default function CustomerLayout() {
  return (
    <div className={styles.container}>
      <Sidebar items={NAV_BY_ROLE.Customer} title="RepairShop" />
      <div className={styles.main}>
        <Header />
        <div className={styles.content}>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
