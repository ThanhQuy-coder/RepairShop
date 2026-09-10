import { useEffect, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { RevenueReport } from '../../types/reports.types';
import { Loading, ErrorMessage, Select, Table, type TableColumn } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';

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
    { key: 'period', header: 'Kỳ', render: (r) => r.period },
    { key: 'ticketCount', header: 'Số hóa đơn', render: (r) => r.ticketCount },
    {
      key: 'totalRevenue',
      header: 'Doanh thu',
      render: (r) => `${r.totalRevenue.toLocaleString('vi-VN')}đ`,
    },
  ];

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Báo cáo doanh thu</h2>

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'flex-end' }}>
        <div>
          <label style={{ fontSize: 13, display: 'block', marginBottom: 4 }}>Từ ngày</label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            style={{ padding: 8, border: '1px solid var(--color-border)', borderRadius: 4 }}
          />
        </div>
        <div>
          <label style={{ fontSize: 13, display: 'block', marginBottom: 4 }}>Đến ngày</label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            style={{ padding: 8, border: '1px solid var(--color-border)', borderRadius: 4 }}
          />
        </div>
        <Select
          options={[
            { value: 'day', label: 'Theo ngày' },
            { value: 'month', label: 'Theo tháng' },
          ]}
          value={groupBy}
          onChange={(e) => setGroupBy(e.target.value as 'day' | 'month')}
        />
      </div>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchReport} />}

      {report && (
        <>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(4, 1fr)',
              gap: 12,
              marginBottom: 24,
            }}
          >
            <SummaryCard
              icon="💰"
              label="Tổng doanh thu"
              value={report.totalRevenue}
              isEmphasized
            />
            <SummaryCard icon="🧾" label="Tổng hóa đơn" value={report.totalInvoices} />
            <SummaryCard icon="✅" label="Đã thanh toán" value={report.paidInvoices} />
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
