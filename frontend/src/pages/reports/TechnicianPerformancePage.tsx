import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { TechnicianSummaryItem } from '../../types/reports.types';
import { Loading, ErrorMessage, Table, type TableColumn } from '../../components/common';

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
    { key: 'technicianName', header: 'Kỹ thuật viên', render: (t) => t.technicianName },
    { key: 'completed', header: 'Đã hoàn thành', render: (t) => t.completed },
    { key: 'inProgress', header: 'Đang xử lý', render: (t) => t.inProgress },
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
    <div>
      <h2 style={{ marginBottom: 16 }}>Hiệu suất kỹ thuật viên</h2>
      <Table
        columns={columns}
        data={items}
        keyExtractor={(t) => t.technicianName}
        emptyMessage="Chưa có kỹ thuật viên nào hoạt động."
      />
    </div>
  );
}
