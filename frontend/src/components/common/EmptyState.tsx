import type { ReactNode } from 'react';
import styles from './EmptyState.module.css';

interface EmptyStateProps {
  message?: string;
  action?: ReactNode;
}

export default function EmptyState({ message = 'Không có dữ liệu', action }: EmptyStateProps) {
  return (
    <div className={styles.wrapper}>
      <p>{message}</p>
      {action}
    </div>
  );
}
