import type { ReactNode } from 'react';
import styles from './Table.module.css';
import Loading from './Loading';
import EmptyState from './EmptyState';

export interface TableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  width?: string;
}

interface TableProps<T> {
  columns: TableColumn<T>[];
  data: T[];
  keyExtractor: (row: T) => string;
  isLoading?: boolean;
  emptyMessage?: string;
  onRowClick?: (row: T) => void;
}

// Generic component — 1 Table dùng được cho MỌI loại dữ liệu (Customer, Device, Ticket...),
// đúng nguyên tắc "không viết <table> 20 lần cho 20 page khác nhau".
export default function Table<T>({
  columns,
  data,
  keyExtractor,
  isLoading,
  emptyMessage = 'Không có dữ liệu',
  onRowClick,
}: TableProps<T>) {
  if (isLoading) return <Loading />;
  if (data.length === 0) return <EmptyState message={emptyMessage} />;

  return (
    <div className={styles.wrapper}>
      <table className={styles.table}>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.key} style={{ width: col.width }}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row) => (
            <tr
              key={keyExtractor(row)}
              onClick={() => onRowClick?.(row)}
              className={onRowClick ? styles.clickableRow : ''}
            >
              {columns.map((col) => (
                <td key={col.key}>{col.render(row)}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
