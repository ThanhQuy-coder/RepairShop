import styles from './ErrorMessage.module.css';

interface ErrorMessageProps {
  message: string;
  errors?: string[]; // khớp ApiErrorResponse.errors[] (Task 8, Tuần 3)
}

export default function ErrorMessage({ message, errors }: ErrorMessageProps) {
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
    </div>
  );
}
