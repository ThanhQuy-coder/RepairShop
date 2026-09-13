import { useCallback, useEffect, useState } from 'react';
import { partService } from '../../services/partService';
import { extractApiError } from '../../utils/apiError';
import { useDebounce } from '../../hooks/useDebounce';
import type { Part } from '../../types/inventory.types';
import {
  Button,
  Input,
  Table,
  Pagination,
  ErrorMessage,
  Badge,
  type TableColumn,
} from '../../components/common';
import PartFormModal from '../../components/inventory/PartFormModal';
import StockTransactionModal from '../../components/inventory/StockTransactionModal';
import { inventoryService } from '../../services/inventoryService';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './Inventory.module.css';

const PAGE_SIZE = 15;

export default function PartsListPage() {
  const [parts, setParts] = useState<Part[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400);

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingPart, setEditingPart] = useState<Part | null>(null);
  const [stockPart, setStockPart] = useState<Part | null>(null);
  const [summary, setSummary] = useState<{
    totalParts: number;
    lowStockCount: number;
    outOfStockCount: number;
  } | null>(null);
  const [stockFilter, setStockFilter] = useState<'All' | 'LowStock' | 'OutOfStock'>('All');

  const fetchParts = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      if (stockFilter === 'All') {
        const res = await partService.list({ search: debouncedSearch, page, pageSize: PAGE_SIZE });
        setParts(res.items);
        setTotal(res.total);
      } else {
        const res = await inventoryService.getLowStockParts(stockFilter);
        setParts(res);
        setTotal(res.length);
      }
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [debouncedSearch, page, stockFilter]);

  useEffect(() => {
    fetchParts();
  }, [fetchParts]);
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const columns: TableColumn<Part>[] = [
    {
      key: 'sku',
      header: 'Mã SKU',
      render: (p) => <span className={styles.skuTag}>{p.sku}</span>,
    },
    { key: 'name', header: 'Tên linh kiện', render: (p) => <strong>{p.name}</strong> },
    { key: 'category', header: 'Danh mục', render: (p) => p.category ?? '—' },
    {
      key: 'unitPrice',
      header: 'Giá bán',
      render: (p) => `${p.unitPrice.toLocaleString('vi-VN')}đ`,
    },
    {
      key: 'quantityOnHand',
      header: 'Tồn kho',
      render: (p) => (
        <span className={styles.stockQty}>
          <span>{p.quantityOnHand}</span>
          {p.isLowStock && <Badge variant="danger">Sắp hết</Badge>}
        </span>
      ),
    },
    {
      key: 'stockActions',
      header: '',
      width: '180px',
      render: (p) => (
        <div className={styles.actionBtns}>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setStockPart(p);
            }}
          >
            Nhập kho
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setEditingPart(p);
              setIsFormOpen(true);
            }}
          >
            Sửa
          </Button>
        </div>
      ),
    },
  ];

  useEffect(() => {
    inventoryService.getDashboard().then(setSummary);
  }, [parts]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>QUẢN TRỊ KHO</span>
          <h1 className={styles.title}>Kho linh kiện</h1>
          <p className={styles.subtitle}>Kiểm soát số lượng tồn kho, định giá và quản lý nhập linh kiện thay thế.</p>
        </div>
        <Button
          size="md"
          onClick={() => {
            setEditingPart(null);
            setIsFormOpen(true);
          }}
        >
          + Thêm linh kiện
        </Button>
      </div>

      {summary && (
        <div className={styles.summaryGrid}>
          <SummaryCard icon="📦" label="Tổng số linh kiện" value={summary.totalParts} isEmphasized />
          <SummaryCard icon="⚠️" label="Linh kiện sắp hết hàng" value={summary.lowStockCount} />
          <SummaryCard icon="❌" label="Linh kiện hết hàng" value={summary.outOfStockCount} />
        </div>
      )}

      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchParts} />}

      <div className={styles.controlCard}>
        <div className={styles.searchBar}>
          <Input
            placeholder="Tìm theo tên linh kiện hoặc mã SKU..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className={styles.filterChips}>
          <button
            type="button"
            className={`${styles.chip} ${stockFilter === 'All' ? styles.chipActive : ''}`}
            onClick={() => setStockFilter('All')}
          >
            Tất cả
          </button>
          <button
            type="button"
            className={`${styles.chip} ${stockFilter === 'LowStock' ? styles.chipActive : ''}`}
            onClick={() => setStockFilter('LowStock')}
          >
            ⚠️ Sắp hết hàng
          </button>
          <button
            type="button"
            className={`${styles.chip} ${stockFilter === 'OutOfStock' ? styles.chipActive : ''}`}
            onClick={() => setStockFilter('OutOfStock')}
          >
            ❌ Hết hàng
          </button>
        </div>
      </div>

      <Table
        columns={columns}
        data={parts}
        keyExtractor={(p) => p.id}
        isLoading={isLoading}
        emptyMessage="Chưa có linh kiện nào phù hợp."
      />

      <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <PartFormModal
        isOpen={isFormOpen}
        editingPart={editingPart}
        onClose={() => setIsFormOpen(false)}
        onSaved={fetchParts}
      />

      <StockTransactionModal
        isOpen={!!stockPart}
        part={stockPart}
        onClose={() => setStockPart(null)}
        onDone={() => {
          setStockPart(null);
          fetchParts();
        }}
      />
    </div>
  );
}

