import { useEffect, useState } from 'react';
import { inventoryService } from '../../services/inventoryService';
import { extractApiError } from '../../utils/apiError';
import type { InventoryTransactionItem } from '../../types/inventory.types';
import { Table, Badge, ErrorMessage, Pagination, type TableColumn } from '../../components/common';

const TYPE_LABEL: Record<string, string> = {
  Import: 'Nhập kho',
  Export: 'Xuất kho (sửa chữa)',
  Adjustment: 'Điều chỉnh',
};
const TYPE_VARIANT: Record<string, 'success' | 'warning' | 'info'> = {
  Import: 'success',
  Export: 'warning',
  Adjustment: 'info',
};

const PAGE_SIZE = 20;

export default function InventoryTransactionsPage() {
  const [items, setItems] = useState<InventoryTransactionItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchData = () => {
    setIsLoading(true);
    setErrorMessage(null);
    inventoryService
      .getTransactions({ page, pageSize: PAGE_SIZE })
      .then((res) => {
        setItems(res.items);
        setTotal(res.total);
      })
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchData, [page]);

  const columns: TableColumn<InventoryTransactionItem>[] = [
    {
      key: 'createdAt',
      header: 'Thời gian',
      render: (t) => new Date(t.createdAt).toLocaleString('vi-VN'),
    },
    { key: 'partName', header: 'Linh kiện', render: (t) => t.partName },
    {
      key: 'type',
      header: 'Loại',
      render: (t) => <Badge variant={TYPE_VARIANT[t.type]}>{TYPE_LABEL[t.type]}</Badge>,
    },
    { key: 'quantity', header: 'Số lượng', render: (t) => t.quantity },
    { key: 'performedByName', header: 'Người thực hiện', render: (t) => t.performedByName },
  ];

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Lịch sử nhập/xuất kho</h2>
      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchData} />}
      <Table
        columns={columns}
        data={items}
        keyExtractor={(t) => t.id}
        isLoading={isLoading}
        emptyMessage="Chưa có giao dịch kho nào."
      />
      <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />
    </div>
  );
}
