import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { userService } from '../../services/userService';
import { extractApiError } from '../../utils/apiError';
import { useAuth } from '../../hooks/useAuth';
import type { TicketListItem } from '../../types/ticket.types';
import type { UserListItem } from '../../types/user.types';
import type { Customer } from '../../types/customer.types';
import { TICKET_STATUS_LABELS } from '../../constants/ticketStatus';
import {
  Button,
  Select,
  Table,
  Pagination,
  ErrorMessage,
  type TableColumn,
} from '../../components/common';
import CustomerPicker from '../../components/customer/CustomerPicker';
import TicketStatusBadge from '../../components/ticket/TicketStatusBadge';
import styles from './TicketListPage.module.css';

const PAGE_SIZE = 15;

const STATUS_OPTIONS = Object.entries(TICKET_STATUS_LABELS).map(([value, label]) => ({
  value,
  label,
}));

export default function TicketListPage() {
  const navigate = useNavigate();
  const { role } = useAuth();

  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);

  const [status, setStatus] = useState('');
  const [technicianId, setTechnicianId] = useState('');
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);

  const [technicians, setTechnicians] = useState<UserListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Chỉ Receptionist/Admin cần lọc theo Technician — Technician đăng nhập thì Backend
  // đã tự động chỉ trả ticket của họ (ownership filter ở SearchAsync), filter này vô nghĩa với họ.
  useEffect(() => {
    if (role === 'Receptionist' || role === 'Admin') {
      userService
        .list({ role: 'Technician', isActive: true, pageSize: 100 })
        .then((res) => setTechnicians(res.items));
    }
  }, [role]);

  const fetchTickets = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const result = await ticketService.list({
        status: status || undefined,
        technicianId: technicianId || undefined,
        customerId: selectedCustomer?.id,
        page,
        pageSize: PAGE_SIZE,
      });
      setTickets(result.items);
      setTotal(result.total);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [status, technicianId, selectedCustomer, page]);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);
  useEffect(() => {
    setPage(1);
  }, [status, technicianId, selectedCustomer]);

  const columns: TableColumn<TicketListItem>[] = [
    { key: 'ticketCode', header: 'Mã phiếu', render: (t) => <strong>{t.ticketCode}</strong> },
    { key: 'customerName', header: 'Khách hàng', render: (t) => t.customerName },
    { key: 'deviceLabel', header: 'Thiết bị', render: (t) => t.deviceLabel },
    { key: 'technicianName', header: 'Kỹ thuật viên', render: (t) => t.technicianName ?? '—' },
    { key: 'status', header: 'Trạng thái', render: (t) => <TicketStatusBadge status={t.status} /> },
    {
      key: 'receivedAt',
      header: 'Ngày tạo',
      render: (t) => new Date(t.receivedAt).toLocaleDateString('vi-VN'),
    },
  ];

  const clearFilters = () => {
    setStatus('');
    setTechnicianId('');
    setSelectedCustomer(null);
  };

  return (
    <div>
      <div className={styles.header}>
        <h2>Phiếu sửa chữa</h2>
        {(role === 'Receptionist' || role === 'Admin') && (
          <Button onClick={() => navigate('/tickets/create')}>+ Tiếp nhận thiết bị</Button>
        )}
      </div>

      <div className={styles.filterBar}>
        <Select
          options={STATUS_OPTIONS}
          placeholder="-- Tất cả trạng thái --"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        />

        {(role === 'Receptionist' || role === 'Admin') && (
          <Select
            options={technicians.map((t) => ({ value: t.id, label: t.fullName }))}
            placeholder="-- Tất cả kỹ thuật viên --"
            value={technicianId}
            onChange={(e) => setTechnicianId(e.target.value)}
          />
        )}

        <div className={styles.customerFilter}>
          {selectedCustomer ? (
            <div className={styles.selectedCustomer}>
              <span>{selectedCustomer.fullName}</span>
              <button onClick={() => setSelectedCustomer(null)}>×</button>
            </div>
          ) : (
            <CustomerPicker onSelect={setSelectedCustomer} placeholder="Lọc theo khách hàng..." />
          )}
        </div>

        {(status || technicianId || selectedCustomer) && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            Xoá bộ lọc
          </Button>
        )}
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <Table
        columns={columns}
        data={tickets}
        keyExtractor={(t) => t.id}
        isLoading={isLoading}
        emptyMessage="Không có phiếu sửa chữa nào phù hợp."
        onRowClick={(t) => navigate(`/tickets/${t.id}`)}
      />

      <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />
    </div>
  );
}
