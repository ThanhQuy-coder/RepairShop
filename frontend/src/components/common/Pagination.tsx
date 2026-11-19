import Button from './Button';
import styles from './Pagination.module.css';

interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
}

function getPageNumbers(currentPage: number, totalPages: number): Array<number | 'ellipsis-left' | 'ellipsis-right'> {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  const pages: Array<number | 'ellipsis-left' | 'ellipsis-right'> = [1];
  const start = Math.max(2, currentPage - 1);
  const end = Math.min(totalPages - 1, currentPage + 1);

  if (start > 2) {
    pages.push('ellipsis-left');
  }

  for (let p = start; p <= end; p += 1) {
    pages.push(p);
  }

  if (end < totalPages - 1) {
    pages.push('ellipsis-right');
  }

  pages.push(totalPages);
  return pages;
}

export default function Pagination({ page, pageSize, total, onPageChange }: PaginationProps) {
  if (total <= 0) return null;

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(Math.max(1, page), totalPages);
  const pageNumbers = getPageNumbers(safePage, totalPages);

  return (
    <div className={styles.wrapper} role="navigation" aria-label="Phân trang">
      <div className={styles.controls}>
        <Button
          variant="secondary"
          size="sm"
          disabled={safePage <= 1}
          onClick={() => onPageChange(safePage - 1)}
        >
          ← Trước
        </Button>

        <div className={styles.pageList}>
          {pageNumbers.map((item) =>
            typeof item === 'number' ? (
              <button
                key={item}
                type="button"
                className={`${styles.pageBtn} ${item === safePage ? styles.pageBtnActive : ''}`}
                onClick={() => onPageChange(item)}
                aria-current={item === safePage ? 'page' : undefined}
              >
                {item}
              </button>
            ) : (
              <span key={item} className={styles.ellipsis} aria-hidden="true">
                …
              </span>
            )
          )}
        </div>

        <Button
          variant="secondary"
          size="sm"
          disabled={safePage >= totalPages}
          onClick={() => onPageChange(safePage + 1)}
        >
          Sau →
        </Button>
      </div>

      <span className={styles.info}>
        Trang {safePage} / {totalPages} ({total} kết quả)
      </span>
    </div>
  );
}
