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

  const fetchParts = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const res = await partService.list({ search: debouncedSearch, page, pageSize: PAGE_SIZE });
      setParts(res.items);
      setTotal(res.total);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [debouncedSearch, page]);

  useEffect(() => {
    fetchParts();
  }, [fetchParts]);
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const columns: TableColumn<Part>[] = [
    { key: 'sku', header: 'SKU', render: (p) => p.sku },
    { key: 'name', header: 'Tên linh kiện', render: (p) => p.name },
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
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          {p.quantityOnHand}
          {p.isLowStock && <Badge variant="danger">Sắp hết</Badge>}
        </span>
      ),
    },
    {
      key: 'stockActions',
      header: '',
      width: '160px',
      render: (p) => (
        <div style={{ display: 'flex', gap: 6 }}>
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

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h2>Linh kiện</h2>
        <Button
          onClick={() => {
            setEditingPart(null);
            setIsFormOpen(true);
          }}
        >
          + Thêm linh kiện
        </Button>
      </div>

      <div style={{ maxWidth: 360, marginBottom: 16 }}>
        <Input
          placeholder="Tìm theo tên hoặc SKU..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchParts} />}

      <Table
        columns={columns}
        data={parts}
        keyExtractor={(p) => p.id}
        isLoading={isLoading}
        emptyMessage="Chưa có linh kiện nào."
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
