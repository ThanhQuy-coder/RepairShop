import { Link } from 'react-router-dom';
import { Button } from '../../components/common';
import styles from './ErrorPage.module.css';

export default function UnauthorizedPage() {
  return (
    <div className={styles.wrapper}>
      <h1 className={styles.code}>403</h1>
      <p className={styles.message}>Bạn không có quyền truy cập trang này.</p>
      <Link to="/dashboard">
        <Button>Về trang chính</Button>
      </Link>
    </div>
  );
}
