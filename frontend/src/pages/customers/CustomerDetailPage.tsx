import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { customerService } from '../../services/customerService';
import { deviceService } from '../../services/deviceService';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { Customer } from '../../types/customer.types';
import type { Device } from '../../types/device.types';
import type { TicketListItem } from '../../types/ticket.types';
import { Button, Loading, ErrorMessage, EmptyState, Badge } from '../../components/common';
import { TICKET_STATUS_LABELS, TICKET_STATUS_BADGE_VARIANT } from '../../constants/ticketStatus';
import CustomerFormModal from '../../components/customer/CustomerFormModal';
import styles from './CustomerDetailPage.module.css';

export default function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [tickets, setTickets] = useState<TicketListItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isEditOpen, setIsEditOpen] = useState(false);

  useEffect(() => {
    if (!id) return;

    const load = async () => {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        const [customerData, deviceData] = await Promise.all([
          customerService.getById(id),
          deviceService.getByCustomerId(id),
        ]);
        setCustomer(customerData);
        setDevices(deviceData);

        const ticketData = await ticketService.getByCustomerId(id);
        setTickets(ticketData);
      } catch (err) {
        setErrorMessage(extractApiError(err).message);
      } finally {
        setIsLoading(false);
      }
    };

    load();
  }, [id]);

  if (isLoading) return <Loading />;
  if (errorMessage || !customer)
    return <ErrorMessage message={errorMessage ?? 'Không tìm thấy khách hàng.'} />;

  return (
    <div>
      <div className={styles.header}>
        <Button variant="ghost" size="sm" onClick={() => navigate('/customers')}>
          ← Danh sách khách hàng
        </Button>
        <Button variant="secondary" size="sm" onClick={() => setIsEditOpen(true)}>
          Sửa thông tin
        </Button>
      </div>

      {/* Information */}
      <section className={styles.section}>
        <h3>Thông tin khách hàng</h3>
        <div className={styles.infoGrid}>
          <div>
            <span className={styles.infoLabel}>Họ tên</span>
            <p>{customer.fullName}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Số điện thoại</span>
            <p>{customer.phone}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Email</span>
            <p>{customer.email ?? '—'}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Địa chỉ</span>
            <p>{customer.address ?? '—'}</p>
          </div>
        </div>
      </section>

      {/* Devices */}
      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3>Thiết bị ({devices.length})</h3>
          <Button size="sm" onClick={() => navigate(`/devices?customerId=${customer.id}`)}>
            + Thêm thiết bị
          </Button>
        </div>

        {devices.length === 0 ? (
          <EmptyState message="Khách hàng chưa có thiết bị nào." />
        ) : (
          <div className={styles.deviceGrid}>
            {devices.map((d) => (
              <div
                key={d.id}
                className={styles.deviceCard}
                onClick={() => navigate(`/devices/${d.id}`)}
              >
                <strong>
                  {d.brand} {d.model}
                </strong>
                <span className={styles.deviceMeta}>
                  {d.deviceType} · {d.serialNumber ?? 'Không có IMEI/Serial'}
                </span>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Repair History */}
      <section className={styles.section}>
        <h3>Lịch sử sửa chữa ({tickets.length})</h3>
        {tickets.length === 0 ? (
          <EmptyState message="Chưa có lịch sử sửa chữa." />
        ) : (
          <div className={styles.ticketList}>
            {tickets.map((t) => (
              <div
                key={t.id}
                className={styles.ticketRow}
                onClick={() => navigate(`/tickets/${t.id}`)}
              >
                <span className={styles.ticketCode}>{t.ticketCode}</span>
                <span>{t.issueReported}</span>
                <Badge variant={TICKET_STATUS_BADGE_VARIANT[t.status]}>
                  {TICKET_STATUS_LABELS[t.status]}
                </Badge>
              </div>
            ))}
          </div>
        )}
      </section>

      <CustomerFormModal
        isOpen={isEditOpen}
        editingCustomer={customer}
        onClose={() => setIsEditOpen(false)}
        onSaved={(updated) => setCustomer(updated)}
      />
    </div>
  );
}
