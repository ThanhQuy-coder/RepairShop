import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Badge, ErrorMessage, Input, Loading } from '../../components/common';
import { quoteService } from '../../services/quoteService';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { Quote } from '../../types/quote.types';
import type { TicketListItem } from '../../types/ticket.types';
import styles from './QuotesPage.module.css';

interface QuoteRow extends Quote {
  ticketCode: string;
  customerName: string;
  deviceLabel: string;
}

const statusLabel = { Pending: 'Chờ xác nhận', Approved: 'Đã duyệt', Rejected: 'Đã từ chối' };
const statusVariant = { Pending: 'warning', Approved: 'success', Rejected: 'danger' } as const;

export default function QuotesPage() {
  const navigate = useNavigate();
  const [rows, setRows] = useState<QuoteRow[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'all' | Quote['status']>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.resolve()
      .then(async () => {
        const tickets = await ticketService.list({ page: 1, pageSize: 100 });
        const results = await Promise.all(
          tickets.items.map(async (ticket: TicketListItem) => {
            const quotes = await quoteService.getByTicketId(ticket.id);
            return quotes.map((quote) => ({
              ...quote,
              ticketCode: ticket.ticketCode,
              customerName: ticket.customerName,
              deviceLabel: ticket.deviceLabel,
            }));
          })
        );
        if (!cancelled) setRows(results.flat());
      })
      .catch((err) => !cancelled && setError(extractApiError(err).message))
      .finally(() => !cancelled && setIsLoading(false));
    return () => { cancelled = true; };
  }, []);

  const filteredRows = useMemo(() => {
    const query = search.trim().toLowerCase();
    return rows.filter((row) => {
      const matchesSearch = !query || [row.ticketCode, row.customerName, row.deviceLabel, row.description]
        .some((value) => value.toLowerCase().includes(query));
      return matchesSearch && (status === 'all' || row.status === status);
    });
  }, [rows, search, status]);

  if (isLoading) return <Loading message="Đang tải danh sách báo giá..." />;

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div><p className={styles.eyebrow}>TÀI CHÍNH / DỊCH VỤ</p><h1>Báo giá</h1><p>Kiểm soát báo giá theo từng phiếu sửa chữa và trạng thái xác nhận.</p></div>
        <div className={styles.summary}><strong>{rows.length}</strong><span>tổng báo giá</span></div>
      </header>
      {error && <ErrorMessage message={error} />}
      <section className={styles.toolbar}>
        <Input aria-label="Tìm kiếm báo giá" placeholder="Tìm theo mã phiếu, khách hàng..." value={search} onChange={(event) => setSearch(event.target.value)} />
        <select value={status} onChange={(event) => setStatus(event.target.value as typeof status)} aria-label="Lọc trạng thái">
          <option value="all">Tất cả trạng thái</option><option value="Pending">Chờ xác nhận</option><option value="Approved">Đã duyệt</option><option value="Rejected">Đã từ chối</option>
        </select>
      </section>
      <section className={styles.tableCard}>
        <div className={styles.tableHead}><strong>Danh sách báo giá</strong><span>{filteredRows.length} kết quả</span></div>
        {filteredRows.length === 0 ? <p className={styles.empty}>Chưa có báo giá phù hợp với bộ lọc hiện tại.</p> : <div className={styles.tableWrap}><table><thead><tr><th>Mã phiếu</th><th>Khách hàng / thiết bị</th><th>Nội dung</th><th>Giá trị</th><th>Trạng thái</th><th /></tr></thead><tbody>{filteredRows.map((row) => <tr key={row.id} onClick={() => navigate(`/tickets/${row.ticketId}`)}><td><strong>{row.ticketCode}</strong><small>{new Date(row.createdAt).toLocaleDateString('vi-VN')}</small></td><td><strong>{row.customerName}</strong><small>{row.deviceLabel}</small></td><td>{row.description}<small>{row.items.length} hạng mục</small></td><td className={styles.amount}>{row.totalAmount.toLocaleString('vi-VN')}đ</td><td><Badge variant={statusVariant[row.status]}>{statusLabel[row.status]}</Badge></td><td className={styles.open}>Xem →</td></tr>)}</tbody></table></div>}
      </section>
    </div>
  );
}