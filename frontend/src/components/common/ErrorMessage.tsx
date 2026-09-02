import { Button } from './index';
import styles from './ErrorMessage.module.css';

interface ErrorMessageProps {
  message: string;
  errors?: string[];
  onRetry?: () => void; // mới — nếu truyền vào, hiện nút "Thử lại"
}

export default function ErrorMessage({ message, errors, onRetry }: ErrorMessageProps) {
  return (
    <div className={styles.wrapper}>
      <p className={styles.message}>{message}</p>
      {errors && errors.length > 0 && (
        <ul className={styles.list}>
          {errors.map((err, i) => (
            <li key={i}>{err}</li>
          ))}
        </ul>
      )}
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry} className={styles.retryButton}>
          Thử lại
        </Button>
      )}
    </div>
  );
}
