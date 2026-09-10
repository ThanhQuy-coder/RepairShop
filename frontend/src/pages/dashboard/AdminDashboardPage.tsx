import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { DashboardSummary } from '../../types/reports.types';
import { Loading, ErrorMessage } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './DashboardPage.module.css';

export default function AdminDashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchSummary = () => {
    setIsLoading(true);
    setErrorMessage(null);
    reportsService
      .getDashboardSummary()
      .then(setSummary)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchSummary, []);

  if (isLoading) return <Loading />;
  if (errorMessage || !summary)
    return (
      <ErrorMessage message={errorMessage ?? 'Không tải được dữ liệu.'} onRetry={fetchSummary} />
    );

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Tổng quan quản trị</h2>

      <h3 style={{ marginBottom: 8 }}>Phiếu sửa chữa</h3>
      <div className={styles.grid}>
        <SummaryCard icon="📋" label="Tổng số" value={summary.repair.totalTickets} isEmphasized />
        <SummaryCard icon="⏳" label="Đang chờ xử lý" value={summary.repair.pending} />
        <SummaryCard icon="🔧" label="Đang sửa chữa" value={summary.repair.inRepair} />
        <SummaryCard icon="🎉" label="Đã hoàn thành" value={summary.repair.completed} />
        <SummaryCard icon="❌" label="Đã hủy" value={summary.repair.cancelled} />
      </div>

      <h3 style={{ marginBottom: 8 }}>Doanh thu</h3>
      <div className={styles.grid}>
        <SummaryCard icon="💰" label="Hôm nay" value={summary.revenue.today} />
        <SummaryCard icon="💰" label="Tuần này" value={summary.revenue.thisWeek} />
        <SummaryCard icon="💰" label="Tháng này" value={summary.revenue.thisMonth} isEmphasized />
      </div>

      <h3 style={{ marginBottom: 8 }}>Kỹ thuật viên</h3>
      <div
        style={{
          border: '1px solid var(--color-border)',
          borderRadius: 8,
          overflow: 'hidden',
          marginBottom: 24,
        }}
      >
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ background: 'var(--color-bg-subtle)' }}>
              <th style={{ textAlign: 'left', padding: 10 }}>Kỹ thuật viên</th>
              <th style={{ textAlign: 'right', padding: 10 }}>Hoàn thành</th>
              <th style={{ textAlign: 'right', padding: 10 }}>Đang xử lý</th>
              <th style={{ textAlign: 'right', padding: 10 }}>TG xử lý TB (giờ)</th>
            </tr>
          </thead>
          <tbody>
            {summary.technicians.map((t) => (
              <tr key={t.technicianName} style={{ borderTop: '1px solid var(--color-border)' }}>
                <td style={{ padding: 10 }}>{t.technicianName}</td>
                <td style={{ textAlign: 'right', padding: 10 }}>{t.completed}</td>
                <td style={{ textAlign: 'right', padding: 10 }}>{t.inProgress}</td>
                <td style={{ textAlign: 'right', padding: 10 }}>
                  {t.averageCompletionHours?.toFixed(1) ?? '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h3 style={{ marginBottom: 8 }}>Kho linh kiện</h3>
      <div className={styles.grid}>
        <SummaryCard icon="📦" label="Tổng linh kiện" value={summary.inventory.totalParts} />
        <SummaryCard icon="⚠️" label="Sắp hết hàng" value={summary.inventory.lowStock} />
        <SummaryCard icon="❌" label="Hết hàng" value={summary.inventory.outOfStock} />
      </div>
    </div>
  );
}
