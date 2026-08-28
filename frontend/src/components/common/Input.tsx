import { type InputHTMLAttributes, forwardRef } from 'react';
import styles from './Input.module.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  errorMessage?: string;
}

// forwardRef vì react-hook-form (nếu dùng ở Task sau) cần ref trực tiếp tới thẻ <input>
const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, errorMessage, id, className, ...rest }, ref) => {
    const inputId = id ?? rest.name;

    return (
      <div className={styles.wrapper}>
        {label && (
          <label htmlFor={inputId} className={styles.label}>
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={`${styles.input} ${errorMessage ? styles.inputError : ''} ${className ?? ''}`}
          {...rest}
        />
        {errorMessage && <span className={styles.errorText}>{errorMessage}</span>}
      </div>
    );
  }
);

Input.displayName = 'Input';
export default Input;
