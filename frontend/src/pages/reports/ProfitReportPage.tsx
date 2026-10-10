import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { ProfitReport as ProfitReportData } from '../../types/reports.types';
import { ErrorMessage, Loading, Table, type TableColumn } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './Reports.module.css';

export default function ProfitReportPage() {
  const [fromDate, setFromDate] = useState(() =>
    new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10)
  );
  const [toDate, setToDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<ProfitReportData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchReport = () => {
    setIsLoading(true);
    setErrorMessage(null);
    reportsService
      .getProfitReport({ fromDate, toDate })
      .then(setReport)
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchReport, [fromDate, toDate]);

  const columns: TableColumn<ProfitReportData['items'][number]>[] = [
    { key: 'ticketCode', header: 'Phiếu', render: (item) => <strong>{item.ticketCode}</strong> },
    { key: 'revenue', header: 'Doanh thu', render: (item) => `${item.revenue.toLocaleString('vi-VN')}đ` },
    { key: 'cost', header: 'Giá vốn', render: (item) => `${item.cost.toLocaleString('vi-VN')}đ` },
    {
      key: 'grossProfit',
      header: 'Lợi nhuận gộp',
      render: (item) => `${item.grossProfit.toLocaleString('vi-VN')}đ`,
    },
    { key: 'marginPercent', header: 'Biên lợi nhuận', render: (item) => `${item.marginPercent.toFixed(2)}%` },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.eyebrow}>BÁO CÁO TÀI CHÍNH</span>
        <h1 className={styles.title}>Profit Dashboard</h1>
        <p className={styles.subtitle}>
          Chỉ tính các hóa đơn đã thanh toán trong khoảng thời gian đã chọn.
        </p>
      </div>
      <div className={styles.filterCard}>
        <div className={styles.dateField}>
          <label className={styles.dateLabel}>Từ ngày</label>
          <input className={styles.dateInput} type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        </div>
        <div className={styles.dateField}>
          <label className={styles.dateLabel}>Đến ngày</label>
          <input className={styles.dateInput} type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        </div>
      </div>
      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchReport} />}
      {report && (
        <>
          <div className={styles.summaryGrid}>
            <SummaryCard icon="💰" label="Doanh thu" value={report.totalRevenue} />
            <SummaryCard icon="📦" label="Giá vốn" value={report.totalCost} />
            <SummaryCard icon="📈" label="Lợi nhuận gộp" value={report.grossProfit} isEmphasized />
            <SummaryCard icon="%" label="Biên lợi nhuận (%)" value={report.marginPercent} />
          </div>
          <Table columns={columns} data={report.items} keyExtractor={(item) => item.ticketCode}
            emptyMessage="Không có hóa đơn đã thanh toán trong khoảng thời gian này." />
        </>
      )}
    </div>
  );
}
