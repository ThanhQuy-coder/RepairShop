import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import { useDebounce } from '../../hooks/useDebounce';
import type { Customer } from '../../types/customer.types';
import {
  Button,
  Input,
  Table,
  Pagination,
  ErrorMessage,
  type TableColumn,
} from '../../components/common';
import CustomerFormModal from '../../components/customer/CustomerFormModal';
import styles from './CustomerListPage.module.css';

const PAGE_SIZE = 10;

export default function CustomerListPage() {
  const navigate = useNavigate();

  const [customers, setCustomers] = useState<Customer[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const debouncedSearch = useDebounce(searchInput, 400);

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);

  const fetchCustomers = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const result = await customerService.list({
        search: debouncedSearch,
        page,
        pageSize: PAGE_SIZE,
      });
      setCustomers(result.items);
      setTotal(result.total);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [debouncedSearch, page]);

  useEffect(() => {
    fetchCustomers();
  }, [fetchCustomers]);

  // Reset về trang 1 mỗi khi đổi từ khóa tìm kiếm — tránh đứng ở trang 5 mà kết quả mới chỉ có 2 trang
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const columns: TableColumn<Customer>[] = [
    { key: 'fullName', header: 'Họ tên', render: (c) => c.fullName },
    { key: 'phone', header: 'Số điện thoại', render: (c) => c.phone },
    { key: 'email', header: 'Email', render: (c) => c.email ?? '—' },
    {
      key: 'actions',
      header: '',
      width: '80px',
      render: (c) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={(e) => {
            e.stopPropagation();
            setEditingCustomer(c);
            setIsFormOpen(true);
          }}
        >
          Sửa
        </Button>
      ),
    },
  ];

  return (
    <div>
      <div className={styles.header}>
        <h2>Khách hàng</h2>
        <Button
          onClick={() => {
            setEditingCustomer(null);
            setIsFormOpen(true);
          }}
        >
          + Tạo khách hàng
        </Button>
      </div>

      <div className={styles.searchBar}>
        <Input
          placeholder="Tìm theo tên hoặc số điện thoại..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
        />
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} onRetry={fetchCustomers} />}

      <Table
        columns={columns}
        data={customers}
        keyExtractor={(c) => c.id}
        isLoading={isLoading}
        emptyMessage="Chưa có khách hàng nào."
        onRowClick={(c) => navigate(`/customers/${c.id}`)}
      />

      <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <CustomerFormModal
        isOpen={isFormOpen}
        editingCustomer={editingCustomer}
        onClose={() => setIsFormOpen(false)}
        onSaved={() => fetchCustomers()}
      />
    </div>
  );
}
