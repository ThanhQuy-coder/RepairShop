import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { quoteService } from '../../services/quoteService';
import { customerService } from '../../services/customerService';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import { useAuth } from '../../hooks/useAuth';
import type { TicketDetail, TicketImage, StatusHistoryItem } from '../../types/ticket.types';
import type { Quote } from '../../types/quote.types';
import type { Customer } from '../../types/customer.types';
import type { Device } from '../../types/device.types';
import { Button, Loading, ErrorMessage } from '../../components/common';
import TicketStatusBadge from '../../components/ticket/TicketStatusBadge';
import TicketTimeline from '../../components/ticket/TicketTimeline';
import TicketImageGallery from '../../components/ticket/TicketImageGallery';
import TicketQuoteSection from '../../components/ticket/TicketQuoteSection';
import TicketActions from '../../components/ticket/TicketActions';
import styles from './TicketDetailPage.module.css';

export default function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { role } = useAuth();

  const [ticket, setTicket] = useState<TicketDetail | null>(null);
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [device, setDevice] = useState<Device | null>(null);
  const [images, setImages] = useState<TicketImage[]>([]);
  const [quotes, setQuotes] = useState<Quote[]>([]);
  const [statusHistory, setStatusHistory] = useState<StatusHistoryItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    if (!id) return;
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const ticketData = await ticketService.getById(id);
      setTicket(ticketData as TicketDetail);

      const [customerData, deviceData, imagesData, quotesData, historyData] = await Promise.all([
        customerService.getById(ticketData.customerId),
        deviceService.getById(ticketData.deviceId),
        ticketService.getImages(id),
        quoteService.getByTicketId(id),
        ticketService.getStatusHistory(id),
      ]);

      setCustomer(customerData);
      setDevice(deviceData);
      setImages(imagesData);
      setQuotes(quotesData);
      setStatusHistory(historyData);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  if (isLoading) return <Loading />;
  if (errorMessage || !ticket)
    return <ErrorMessage message={errorMessage ?? 'Không tìm thấy phiếu sửa chữa.'} />;

  const canCreateQuote = role === 'Receptionist' || role === 'Admin';

  return (
    <div className={styles.wrapper}>
      {/* Ticket Header */}
      <div className={styles.headerCard}>
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
          ← Quay lại
        </Button>
        <div className={styles.headerMain}>
          <h2>{ticket.ticketCode}</h2>
          <TicketStatusBadge status={ticket.status} />
        </div>
        <span className={styles.headerMeta}>
          Tiếp nhận: {new Date(ticket.receivedAt).toLocaleString('vi-VN')}
        </span>
      </div>

      <div className={styles.grid}>
        <div className={styles.mainCol}>
          {/* Customer / Device */}
          <section className={styles.section}>
            <h3>Khách hàng & Thiết bị</h3>
            <div className={styles.infoGrid}>
              <div>
                <span className={styles.infoLabel}>Khách hàng</span>
                <p>
                  {customer?.fullName} — {customer?.phone}
                </p>
              </div>
              <div>
                <span className={styles.infoLabel}>Thiết bị</span>
                <p>
                  {device?.brand} {device?.model} ({device?.deviceType})
                </p>
              </div>
            </div>
          </section>

          {/* Issue / Diagnosis */}
          <section className={styles.section}>
            <h3>Tình trạng & Chẩn đoán</h3>
            <div className={styles.infoGrid}>
              <div className={styles.infoFull}>
                <span className={styles.infoLabel}>Mô tả lỗi khách khai báo</span>
                <p>{ticket.issueReported}</p>
              </div>
              {ticket.conditionNotes && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Tình trạng ban đầu</span>
                  <p>{ticket.conditionNotes}</p>
                </div>
              )}
              {ticket.riskWarning && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Cảnh báo rủi ro</span>
                  <p>{ticket.riskWarning}</p>
                </div>
              )}
              {ticket.diagnosisResult && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Kết quả chẩn đoán</span>
                  <p>{ticket.diagnosisResult}</p>
                </div>
              )}
              {ticket.rootCause && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Nguyên nhân</span>
                  <p>{ticket.rootCause}</p>
                </div>
              )}
              {ticket.completionNotes && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Ghi chú hoàn tất</span>
                  <p>{ticket.completionNotes}</p>
                </div>
              )}
              {ticket.notes && (
                <div className={styles.infoFull}>
                  <span className={styles.infoLabel}>Ghi chú kỹ thuật</span>
                  <p style={{ whiteSpace: 'pre-line' }}>{ticket.notes}</p>
                </div>
              )}
            </div>
          </section>

          {/* Images */}
          <section className={styles.section}>
            <h3>Hình ảnh</h3>
            <TicketImageGallery images={images} />
          </section>

          {/* Quote */}
          <section className={styles.section}>
            <h3>Báo giá</h3>
            <TicketQuoteSection
              ticketId={ticket.id}
              ticketStatus={ticket.status}
              quotes={quotes}
              canCreateQuote={canCreateQuote}
              onQuoteCreated={loadAll}
            />
          </section>
        </div>

        <div className={styles.sideCol}>
          {/* Workflow Timeline */}
          <section className={styles.section}>
            <h3>Quy trình xử lý</h3>
            <TicketTimeline currentStatus={ticket.status} statusHistory={statusHistory} />
          </section>

          {/* Actions */}
          <section className={styles.section}>
            <h3>Hành động</h3>
            <TicketActions ticket={ticket} quotes={quotes} onUpdated={loadAll} />
          </section>
        </div>
      </div>
    </div>
  );
}
