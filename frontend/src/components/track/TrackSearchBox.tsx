import { type FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input } from '../common';
import styles from './TrackSearchBox.module.css';

interface TrackSearchBoxProps {
  initialValue?: string;
}

export default function TrackSearchBox({ initialValue = '' }: TrackSearchBoxProps) {
  const navigate = useNavigate();
  const [code, setCode] = useState(initialValue);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = code.trim().toUpperCase();

    if (!trimmed) {
      setValidationError('Vui lòng nhập mã phiếu sửa chữa.');
      return;
    }

    setValidationError(null);
    navigate(`/track/${trimmed}`);
  };

  return (
    <form onSubmit={handleSubmit} className={styles.wrapper}>
      <h2 className={styles.title}>Tra cứu tiến độ sửa chữa</h2>
      <p className={styles.subtitle}>
        Nhập mã phiếu được cung cấp lúc bàn giao thiết bị để xem tiến độ hiện tại.
      </p>

      <div className={styles.searchRow}>
        <Input
          placeholder="VD: RT-20260821-5794"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          errorMessage={validationError ?? undefined}
        />
        <Button type="submit">Tra cứu</Button>
      </div>
    </form>
  );
}
