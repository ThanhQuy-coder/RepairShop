import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { TechnicianSummaryItem } from '../../types/reports.types';
import { Loading, ErrorMessage, Table, type TableColumn } from '../../components/common';

import styles from './Reports.module.css';

export default function TechnicianPerformancePage() {
  const [items, setItems] = useState<TechnicianSummaryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchData = () => {
    setIsLoading(true);
    setErrorMessage(null);
    reportsService
      .getTechnicianPerformance({})
      .then(setItems)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchData, []);

  const columns: TableColumn<TechnicianSummaryItem>[] = [
    {
      key: 'technicianName',
      header: 'Kỹ thuật viên',
      render: (t) => (
        <div className={styles.techCell}>
          <div className={styles.techAvatar}>
            {t.technicianName ? t.technicianName[0].toUpperCase() : 'K'}
          </div>
          <strong>{t.technicianName}</strong>
        </div>
      ),
    },
    {
      key: 'completed',
      header: 'Đã hoàn thành',
      render: (t) => (
        <span style={{ fontWeight: 700, color: 'var(--color-success, #16a34a)' }}>
          {t.completed} phiếu
        </span>
      ),
    },
    {
      key: 'inProgress',
      header: 'Đang xử lý',
      render: (t) => (
        <span style={{ fontWeight: 600, color: 'var(--color-warning-hover, #b45309)' }}>
          {t.inProgress} phiếu
        </span>
      ),
    },
    {
      key: 'avgHours',
      header: 'TG xử lý trung bình',
      render: (t) =>
        t.averageCompletionHours != null ? `${t.averageCompletionHours.toFixed(1)} giờ` : '—',
    },
  ];

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} onRetry={fetchData} />;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.eyebrow}>ĐÁNH GIÁ NĂNG SUẤT</span>
        <h1 className={styles.title}>Hiệu suất kỹ thuật viên</h1>
        <p className={styles.subtitle}>Thống kê số lượng phiếu sửa chữa hoàn thành, khối lượng đang thực hiện và thời gian trung bình.</p>
      </div>

      <Table
        columns={columns}
        data={items}
        keyExtractor={(t) => t.technicianName}
        emptyMessage="Chưa có kỹ thuật viên nào hoạt động."
      />
    </div>
  );
}

