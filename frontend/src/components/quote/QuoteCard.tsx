import type { Quote } from '../../types/quote.types';
import { Badge } from '../common';
import styles from './QuoteCard.module.css';

interface QuoteCardProps {
  quote: Quote;
  children?: React.ReactNode; // slot cho action (QuoteApprovalForm) — chỉ Customer mới truyền vào
}

const STATUS_VARIANT: Record<string, 'default' | 'success' | 'danger'> = {
  Pending: 'default',
  Approved: 'success',
  Rejected: 'danger',
};

const STATUS_LABEL: Record<string, string> = {
  Pending: 'Đang chờ xác nhận',
  Approved: 'Đã đồng ý',
  Rejected: 'Đã từ chối',
};

const ITEM_TYPE_LABEL: Record<string, string> = { Service: 'Dịch vụ', Part: 'Linh kiện' };

export default function QuoteCard({ quote, children }: QuoteCardProps) {
  return (
    <div className={styles.card}>
      <div className={styles.header}>
        <div>
          <h4 className={styles.title}>{quote.description}</h4>
          <span className={styles.createdAt}>
            Ngày tạo: {new Date(quote.createdAt).toLocaleDateString('vi-VN')}
          </span>
        </div>
        <Badge variant={STATUS_VARIANT[quote.status]}>{STATUS_LABEL[quote.status]}</Badge>
      </div>

      {/* Diagnosis + Services + Parts + Quantity + Price = Total — đúng bố cục mentor yêu cầu */}
      <table className={styles.itemsTable}>
        <thead>
          <tr>
            <th>Hạng mục</th>
            <th className={styles.colType}>Loại</th>
            <th className={styles.colNum}>SL</th>
            <th className={styles.colNum}>Đơn giá</th>
            <th className={styles.colNum}>Thành tiền</th>
          </tr>
        </thead>
        <tbody>
          {quote.items.map((item) => (
            <tr key={item.id}>
              <td>{item.description}</td>
              <td className={styles.colType}>{ITEM_TYPE_LABEL[item.itemType]}</td>
              <td className={styles.colNum}>{item.quantity}</td>
              <td className={styles.colNum}>{item.unitPrice.toLocaleString('vi-VN')}đ</td>
              <td className={styles.colNum}>{item.subtotal.toLocaleString('vi-VN')}đ</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={4} className={styles.totalLabel}>Tổng cộng</td>
            <td className={styles.totalValue}>{quote.totalAmount.toLocaleString('vi-VN')}đ</td>
          </tr>
        </tfoot>
      </table>

      {children && <div className={styles.actionsSlot}>{children}</div>}
    </div>
  );
}