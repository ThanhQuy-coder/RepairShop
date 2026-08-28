import { type SelectHTMLAttributes, forwardRef } from 'react';
import styles from './Input.module.css'; // dùng chung style với Input — cùng "họ" form field

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  errorMessage?: string;
  options: SelectOption[];
  placeholder?: string;
}

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, errorMessage, options, placeholder, id, className, ...rest }, ref) => {
    const selectId = id ?? rest.name;

    return (
      <div className={styles.wrapper}>
        {label && (
          <label htmlFor={selectId} className={styles.label}>
            {label}
          </label>
        )}
        <select
          ref={ref}
          id={selectId}
          className={`${styles.input} ${errorMessage ? styles.inputError : ''} ${className ?? ''}`}
          {...rest}
        >
          {placeholder && <option value="">{placeholder}</option>}
          {options.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        {errorMessage && <span className={styles.errorText}>{errorMessage}</span>}
      </div>
    );
  }
);

Select.displayName = 'Select';
export default Select;
