import { Link } from 'react-router-dom';
import { Button } from '../../components/common';
import styles from './ErrorPage.module.css';

export default function NotFoundPage() {
  return (
    <div className={styles.wrapper}>
      <h1 className={styles.code}>404</h1>
      <p className={styles.message}>Trang bạn tìm không tồn tại.</p>
      <Link to="/">
        <Button>Về trang chủ</Button>
      </Link>
    </div>
  );
}
