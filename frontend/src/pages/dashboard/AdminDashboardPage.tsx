import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { DashboardSummary } from '../../types/reports.types';
import { Loading, ErrorMessage, Button } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './AdminDashboardPage.module.css';

export default function AdminDashboardPage() {
  const navigate = useNavigate();
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
      <div className={styles.header}>
        <h2>Tổng quan quản trị</h2>
        <div className={styles.shortcutRow}>
          <Button variant="secondary" size="sm" onClick={() => navigate('/admin/reports/revenue')}>
            Báo cáo doanh thu
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate('/admin/reports/technicians')}
          >
            Hiệu suất KTV
          </Button>
          <Button variant="secondary" size="sm" onClick={() => navigate('/admin/inventory')}>
            Kho linh kiện
          </Button>
        </div>
      </div>

      {/* KPI cards hàng ngang — đúng ví dụ mentor: Tickets / Revenue / Customers */}
      <div className={styles.kpiRow}>
        <SummaryCard
          icon="📋"
          label="Phiếu sửa chữa"
          value={summary.repair.totalTickets}
          isEmphasized
        />
        <SummaryCard icon="💰" label="Doanh thu tháng này" value={summary.revenue.thisMonth} />
        <SummaryCard icon="👥" label="Khách hàng" value={summary.totalCustomers} />
      </div>

      {/* Repair Status — bảng phân bố trạng thái, đúng ví dụ mentor vẽ */}
      <section className={styles.section}>
        <h3>Phân bố trạng thái phiếu sửa chữa</h3>
        <div className={styles.statusList}>
          {summary.statusBreakdown.map((s) => (
            <div key={s.statusCode} className={styles.statusRow}>
              <span className={styles.statusLabel}>{s.statusLabel}</span>
              <div className={styles.statusBarTrack}>
                <div
                  className={styles.statusBarFill}
                  style={{
                    width: `${summary.repair.totalTickets > 0 ? (s.count / summary.repair.totalTickets) * 100 : 0}%`,
                  }}
                />
              </div>
              <span className={styles.statusCount}>{s.count}</span>
            </div>
          ))}
        </div>
      </section>

      {/* Inventory summary — tái dùng dữ liệu Task 7.5 */}
      <section className={styles.section}>
        <h3>Kho linh kiện</h3>
        <div className={styles.kpiRow}>
          <SummaryCard icon="📦" label="Tổng linh kiện" value={summary.inventory.totalParts} />
          <SummaryCard icon="⚠️" label="Sắp hết hàng" value={summary.inventory.lowStock} />
          <SummaryCard icon="❌" label="Hết hàng" value={summary.inventory.outOfStock} />
        </div>
      </section>

      {/* Technician performance — bảng thu gọn, chi tiết đầy đủ ở trang riêng (Task 7.8) */}
      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3>Kỹ thuật viên</h3>
          <Button variant="ghost" size="sm" onClick={() => navigate('/admin/reports/technicians')}>
            Xem chi tiết →
          </Button>
        </div>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Kỹ thuật viên</th>
              <th>Hoàn thành</th>
              <th>Đang xử lý</th>
            </tr>
          </thead>
          <tbody>
            {summary.technicians.slice(0, 5).map((t) => (
              <tr key={t.technicianName}>
                <td>{t.technicianName}</td>
                <td>{t.completed}</td>
                <td>{t.inProgress}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
