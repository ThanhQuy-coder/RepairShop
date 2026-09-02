import { useToastStore } from '../../store/toastStore';
import styles from './ToastContainer.module.css';

const ICON: Record<string, string> = { success: '✓', error: '✕', info: 'ℹ' };

export default function ToastContainer() {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) return null;

  return (
    <div className={styles.container}>
      {toasts.map((t) => (
        <div
          key={t.id}
          className={`${styles.toast} ${styles[t.type]}`}
          onClick={() => removeToast(t.id)}
        >
          <span className={styles.icon}>{ICON[t.type]}</span>
          <span>{t.message}</span>
        </div>
      ))}
    </div>
  );
}
