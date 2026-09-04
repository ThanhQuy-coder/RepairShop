import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { deviceService } from '../../services/deviceService';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import type { Device } from '../../types/device.types';
import type { TicketListItem } from '../../types/ticket.types';
import type { Customer } from '../../types/customer.types';
import { Button, Loading, ErrorMessage, EmptyState, Badge } from '../../components/common';
import { TICKET_STATUS_LABELS, TICKET_STATUS_BADGE_VARIANT } from '../../constants/ticketStatus';
import DeviceFormModal from '../../components/device/DeviceFormModal';
import styles from '../customers/CustomerDetailPage.module.css'; // tái dùng layout section đã có

export default function DeviceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [device, setDevice] = useState<Device | null>(null);
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [history, setHistory] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isEditOpen, setIsEditOpen] = useState(false);

  useEffect(() => {
    if (!id) return;

    const load = async () => {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        const deviceData = await deviceService.getById(id);
        setDevice(deviceData);

        const customerData = await customerService.getById(deviceData.customerId);
        setCustomer(customerData);

        const historyData = await deviceService.getRepairHistory(id);
        setHistory(historyData);
      } catch (err) {
        setErrorMessage(extractApiError(err).message);
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [id]);

  if (isLoading) return <Loading />;
  if (errorMessage || !device)
    return <ErrorMessage message={errorMessage ?? 'Không tìm thấy thiết bị.'} />;

  return (
    <div>
      <div className={styles.header}>
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
          ← Quay lại
        </Button>
        <Button variant="secondary" size="sm" onClick={() => setIsEditOpen(true)}>
          Sửa thông tin
        </Button>
      </div>

      <section className={styles.section}>
        <h3>Thông tin thiết bị</h3>
        <div className={styles.infoGrid}>
          <div>
            <span className={styles.infoLabel}>Loại</span>
            <p>{device.deviceType}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Hãng</span>
            <p>{device.brand}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Model</span>
            <p>{device.model}</p>
          </div>
          <div>
            <span className={styles.infoLabel}>Serial/IMEI</span>
            <p>{device.serialNumber ?? '—'}</p>
          </div>
        </div>
      </section>

      <section className={styles.section}>
        <h3>Khách hàng sở hữu</h3>
        {customer ? (
          <Link to={`/customers/${customer.id}`}>
            <strong>{customer.fullName}</strong> — {customer.phone}
          </Link>
        ) : (
          <p>—</p>
        )}
      </section>

      <section className={styles.section}>
        <h3>Lịch sử sửa chữa ({history.length})</h3>
        {history.length === 0 ? (
          <EmptyState message="Thiết bị chưa có lịch sử sửa chữa." />
        ) : (
          <div className={styles.ticketList}>
            {history.map((h) => (
              <div key={h.ticketCode} className={styles.ticketRow}>
                <span className={styles.ticketCode}>{h.ticketCode}</span>
                <span>{h.deviceLabel}</span>
                <Badge variant={TICKET_STATUS_BADGE_VARIANT[h.status]}>
                  {TICKET_STATUS_LABELS[h.status] ?? h.status}
                </Badge>
              </div>
            ))}
          </div>
        )}
      </section>

      <DeviceFormModal
        isOpen={isEditOpen}
        customerId={device.customerId}
        editingDevice={device}
        onClose={() => setIsEditOpen(false)}
        onSaved={(updated) => setDevice(updated)}
      />
    </div>
  );
}
