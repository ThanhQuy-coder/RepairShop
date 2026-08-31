import { useEffect, useState, useRef } from 'react';
import { Input } from '../common';
import { customerService } from '../../services/customerService';
import { useDebounce } from '../../hooks/useDebounce';
import type { Customer } from '../../types/customer.types';
import styles from './CustomerPicker.module.css';

interface CustomerPickerProps {
  onSelect: (customer: Customer) => void;
  placeholder?: string;
}

// Component dùng chung cho MỌI nơi cần "tìm và chọn 1 Customer đã tồn tại" —
// tránh viết lại logic search/debounce/dropdown ở Device Create (5.8) và Intake Wizard (5.9).
export default function CustomerPicker({
  onSelect,
  placeholder = 'Tìm theo tên hoặc SĐT...',
}: CustomerPickerProps) {
  const [search, setSearch] = useState('');
  const [results, setResults] = useState<Customer[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const debounced = useDebounce(search, 350);
  const blurTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!debounced.trim()) {
      setResults([]);
      return;
    }
    setIsLoading(true);
    customerService
      .list({ search: debounced, page: 1, pageSize: 5 })
      .then((res) => setResults(res.items))
      .finally(() => setIsLoading(false));
  }, [debounced]);

  const handleSelect = (customer: Customer) => {
    onSelect(customer);
    setSearch('');
    setIsOpen(false);
  };

  return (
    <div className={styles.wrapper}>
      <Input
        placeholder={placeholder}
        value={search}
        onChange={(e) => {
          setSearch(e.target.value);
          setIsOpen(true);
        }}
        onFocus={() => setIsOpen(true)}
        onBlur={() => {
          blurTimeout.current = setTimeout(() => setIsOpen(false), 150);
        }}
      />
      {isOpen && search && (
        <div className={styles.dropdown}>
          {isLoading && <div className={styles.item}>Đang tìm...</div>}
          {!isLoading && results.length === 0 && (
            <div className={styles.item}>Không tìm thấy khách hàng phù hợp.</div>
          )}
          {results.map((c) => (
            <div key={c.id} className={styles.item} onMouseDown={() => handleSelect(c)}>
              <strong>{c.fullName}</strong> — {c.phone}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
