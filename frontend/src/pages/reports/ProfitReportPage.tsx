import { useEffect, useMemo, useState } from 'react';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type { ProfitReport as ProfitReportData } from '../../types/reports.types';
import {
  ErrorMessage,
  Loading,
  Pagination,
  Table,
  type TableColumn,
} from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './Reports.module.css';

const SERVICE_PAGE_SIZE = 5;
const ITEMS_PAGE_SIZE = 6;

export default function ProfitReportPage() {
  const [fromDate, setFromDate] = useState(() =>
    new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10)
  );
  const [toDate, setToDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<ProfitReportData | null>(null);
  const [servicePage, setServicePage] = useState(1);
  const [itemPage, setItemPage] = useState(1);
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

  useEffect(() => {
    setServicePage(1);
    setItemPage(1);
  }, [fromDate, toDate]);

  const serviceTotal = report?.byService.length ?? 0;
  const serviceTotalPages = Math.max(1, Math.ceil(serviceTotal / SERVICE_PAGE_SIZE));
  const itemTotal = report?.items.length ?? 0;
  const itemTotalPages = Math.max(1, Math.ceil(itemTotal / ITEMS_PAGE_SIZE));

  useEffect(() => {
    if (servicePage > serviceTotalPages) setServicePage(serviceTotalPages);
  }, [servicePage, serviceTotalPages]);

  useEffect(() => {
    if (itemPage > itemTotalPages) setItemPage(itemTotalPages);
  }, [itemPage, itemTotalPages]);

  const pagedServices = useMemo(() => {
    if (!report) return [];
    return report.byService.slice(
      (servicePage - 1) * SERVICE_PAGE_SIZE,
      servicePage * SERVICE_PAGE_SIZE
    );
  }, [report, servicePage]);

  const pagedItems = useMemo(() => {
    if (!report) return [];
    return report.items.slice((itemPage - 1) * ITEMS_PAGE_SIZE, itemPage * ITEMS_PAGE_SIZE);
  }, [report, itemPage]);

  const columns: TableColumn<ProfitReportData['items'][number]>[] = [
    { key: 'ticketCode', header: 'Phiếu', render: (item) => <strong>{item.ticketCode}</strong> },
    {
      key: 'revenue',
      header: 'Doanh thu',
      render: (item) => `${item.revenue.toLocaleString('vi-VN')}đ`,
    },
    { key: 'cost', header: 'Giá vốn', render: (item) => `${item.cost.toLocaleString('vi-VN')}đ` },
    {
      key: 'grossProfit',
      header: 'Lợi nhuận gộp',
      render: (item) => `${item.grossProfit.toLocaleString('vi-VN')}đ`,
    },
    {
      key: 'marginPercent',
      header: 'Biên lợi nhuận',
      render: (item) => `${item.marginPercent.toFixed(2)}%`,
    },
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
          <input
            className={styles.dateInput}
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
          />
        </div>
        <div className={styles.dateField}>
          <label className={styles.dateLabel}>Đến ngày</label>
          <input
            className={styles.dateInput}
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
          />
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
          <section className={styles.breakdownCard}>
            <div className={styles.breakdownHeader}>
              <div>
                <h2 className={styles.sectionTitle}>Lợi nhuận theo dịch vụ</h2>
                <p className={styles.sectionSubtitle}>
                  Phân bổ giá vốn linh kiện theo tỷ trọng doanh thu của từng dịch vụ.
                </p>
              </div>
              <span className={styles.paidOnlyBadge}>Chỉ hóa đơn đã thanh toán</span>
            </div>
            <Table
              columns={[
                {
                  key: 'serviceName',
                  header: 'Dịch vụ / nhóm',
                  render: (item) => <strong>{item.serviceName}</strong>,
                },
                { key: 'ticketCount', header: 'Số ticket', render: (item) => item.ticketCount },
                {
                  key: 'revenue',
                  header: 'Doanh thu',
                  render: (item) => `${item.revenue.toLocaleString('vi-VN')}đ`,
                },
                {
                  key: 'cost',
                  header: 'Giá vốn',
                  render: (item) => `${item.cost.toLocaleString('vi-VN')}đ`,
                },
                {
                  key: 'grossProfit',
                  header: 'Lợi nhuận gộp',
                  render: (item) => `${item.grossProfit.toLocaleString('vi-VN')}đ`,
                },
                {
                  key: 'marginPercent',
                  header: 'Biên lợi nhuận',
                  render: (item) => `${item.marginPercent.toFixed(2)}%`,
                },
              ]}
              data={pagedServices}
              keyExtractor={(item) => item.serviceName}
              emptyMessage="Chưa có dữ liệu dịch vụ trong khoảng thời gian này."
            />
            <Pagination
              page={servicePage}
              pageSize={SERVICE_PAGE_SIZE}
              total={serviceTotal}
              onPageChange={setServicePage}
            />
          </section>
          <section className={styles.breakdownCard}>
            <div className={styles.breakdownHeader}>
              <div>
                <h2 className={styles.sectionTitle}>Chi tiết lợi nhuận theo phiếu sửa chữa</h2>
                <p className={styles.sectionSubtitle}>
                  Danh sách từng phiếu đã thu tiền kèm doanh thu, giá vốn và biên lợi nhuận.
                </p>
              </div>
              <span className={styles.paidOnlyBadge}>{itemTotal} phiếu</span>
            </div>
            <Table
              columns={columns}
              data={pagedItems}
              keyExtractor={(item) => item.ticketCode}
              emptyMessage="Không có hóa đơn đã thanh toán trong khoảng thời gian này."
            />
            <Pagination
              page={itemPage}
              pageSize={ITEMS_PAGE_SIZE}
              total={itemTotal}
              onPageChange={setItemPage}
            />
          </section>
        </>
      )}
    </div>
  );
}
