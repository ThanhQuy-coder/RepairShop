import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { RevenueReport } from '../../types/reports.types';
import { Loading, ErrorMessage, Select, Table, type TableColumn } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';

import styles from './Reports.module.css';

export default function RevenueReportPage() {
  const [fromDate, setFromDate] = useState(() =>
    new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10)
  );
  const [toDate, setToDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [groupBy, setGroupBy] = useState<'day' | 'month'>('day');

  const [report, setReport] = useState<RevenueReport | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchReport = () => {
    setIsLoading(true);
    setErrorMessage(null);
    reportsService
      .getRevenueReport({ fromDate, toDate, groupBy })
      .then(setReport)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchReport, [fromDate, toDate, groupBy]);

  const columns: TableColumn<{ period: string; totalRevenue: number; ticketCount: number }>[] = [
    { key: 'period', header: 'Kỳ thời gian', render: (r) => <strong>{r.period}</strong> },
    { key: 'ticketCount', header: 'Số hóa đơn hoàn tất', render: (r) => r.ticketCount },
    {
      key: 'totalRevenue',
      header: 'Tổng doanh thu',
      render: (r) => (
        <strong style={{ color: 'var(--color-primary, #185a4e)' }}>
          {r.totalRevenue.toLocaleString('vi-VN')}đ
        </strong>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.eyebrow}>BÁO CÁO TÀI CHÍNH</span>
        <h1 className={styles.title}>Báo cáo doanh thu dịch vụ</h1>
        <p className={styles.subtitle}>Phân tích nguồn thu theo mốc thời gian, số lượng hóa đơn và trạng thái thanh toán.</p>
      </div>

      <div className={styles.filterCard}>
        <div className={styles.dateField}>
          <label className={styles.dateLabel}>Từ ngày</label>
          <input
            type="date"
            className={styles.dateInput}
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
          />
        </div>
        <div className={styles.dateField}>
          <label className={styles.dateLabel}>Đến ngày</label>
          <input
            type="date"
            className={styles.dateInput}
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
          />
        </div>
        <div style={{ minWidth: 160 }}>
          <Select
            label="Nhóm theo"
            options={[
              { value: 'day', label: 'Theo từng ngày' },
              { value: 'month', label: 'Theo từng tháng' },
            ]}
            value={groupBy}
            onChange={(e) => setGroupBy(e.target.value as 'day' | 'month')}
          />
        </div>
      </div>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchReport} />}

      {report && (
        <>
          <div className={styles.summaryGrid}>
            <SummaryCard
              icon="💰"
              label="Tổng doanh thu"
              value={report.totalRevenue}
              isEmphasized
            />
            <SummaryCard icon="🧾" label="Tổng hóa đơn" value={report.totalInvoices} />
            <SummaryCard icon="✅" label="Đã thu tiền" value={report.paidInvoices} />
            <SummaryCard icon="⏳" label="Chưa thanh toán" value={report.unpaidInvoices} />
          </div>

          <Table
            columns={columns}
            data={report.items}
            keyExtractor={(r) => r.period}
            emptyMessage="Không có doanh thu trong khoảng thời gian này."
          />
        </>
      )}
    </div>
  );
}

