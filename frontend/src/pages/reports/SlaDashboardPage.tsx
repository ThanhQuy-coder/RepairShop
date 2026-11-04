import { useCallback, useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { SlaSummary } from '../../types/reports.types';
import { ErrorMessage, Loading } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import { useAuth } from '../../hooks/useAuth';
import styles from './SlaDashboardPage.module.css';

export default function SlaDashboardPage() {
  const { role } = useAuth();
  const [summary, setSummary] = useState<SlaSummary | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchSummary = useCallback(() => {
    setErrorMessage(null);
    reportsService
      .getSlaSummary()
      .then(setSummary)
      .catch((error) => setErrorMessage(extractApiError(error).message));
  }, []);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  if (!summary && !errorMessage) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} onRetry={fetchSummary} />;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>THEO DÕI SLA</span>
          <h1 className={styles.title}>{role === 'Admin' ? 'Giám sát thời hạn xử lý' : 'SLA công việc của tôi'}</h1>
          <p className={styles.subtitle}>
          {role === 'Admin'
            ? 'Tổng quan toàn hệ thống theo trạng thái SLA của các phiếu đang hoạt động.'
            : 'Chỉ số chỉ gồm các phiếu được phân công hoặc tiếp nhận bởi tài khoản hiện tại.'}
          </p>
        </div>
      </div>
      <div className={styles.grid}>
        <SummaryCard icon="✅" label="Đúng hạn" value={summary?.onTrack ?? 0} />
        <SummaryCard icon="⏳" label="Sắp quá hạn" value={summary?.dueSoon ?? 0} />
        <SummaryCard icon="🚨" label="Đã quá hạn" value={summary?.overdue ?? 0} isEmphasized />
        <SummaryCard icon="📋" label="SLA đang hoạt động" value={summary?.activeTotal ?? 0} />
      </div>
      {summary?.activeTotal === 0 && (
        <section className={styles.empty}>
          <h2>Chưa có SLA đang hoạt động</h2>
          <p>Các SLA đã hoàn tất không được tính vào báo cáo này.</p>
        </section>
      )}
      {summary && summary.activeTotal > 0 && (
        <section className={styles.insight}>
          <strong>Đọc nhanh:</strong>{' '}
          {summary.overdue > 0
            ? `${summary.overdue} phiếu đã quá hạn, cần ưu tiên xử lý ngay.`
            : summary.dueSoon > 0
              ? `${summary.dueSoon} phiếu sắp quá hạn, nên xử lý trước các phiếu đúng hạn.`
              : 'Tất cả phiếu hiện đang trong thời hạn SLA.'}
        </section>
      )}
    </div>
  );
}
