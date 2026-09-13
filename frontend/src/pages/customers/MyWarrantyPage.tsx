import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import type { MyWarrantyItem } from '../../types/warranty.types';
import { Loading, ErrorMessage, EmptyState, Badge, Button } from '../../components/common';
import WarrantyClaimModal from '../../components/warranty/WarrantyClaimModal';
import styles from './MyWarrantyPage.module.css';

export default function MyWarrantyPage() {
  const navigate = useNavigate();
  const [warranties, setWarranties] = useState<MyWarrantyItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [claimingTicket, setClaimingTicket] = useState<MyWarrantyItem | null>(null);

  const fetchData = () => {
    setIsLoading(true);
    setErrorMessage(null);
    warrantyService.getMyWarranties()
      .then(setWarranties)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchData, []);

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} onRetry={fetchData} />;
  if (warranties.length === 0) return <EmptyState message="Bạn chưa có bảo hành nào." />;

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <span className={styles.eyebrow}>TRUNG TÂM BẢO HÀNH</span>
        <h1 className={styles.title}>Bảo hành của tôi</h1>
        <p className={styles.subtitle}>Danh sách thiết bị và chính sách bảo hành đang áp dụng cho tài khoản của bạn</p>
      </header>

      <div className={styles.list}>
        {warranties.map((w) => (
          <div key={w.warrantyCode} className={styles.card}>
            <div className={styles.cardHeader}>
              <div className={styles.deviceInfo}>
                <div className={styles.shieldIcon}>🛡️</div>
                <div>
                  <div className={styles.deviceTitle}>{w.deviceLabel}</div>
                  <span className={styles.warrantyCodeTag}>Mã: {w.warrantyCode}</span>
                </div>
              </div>
              <Badge variant={w.isExpired || w.status === 'Voided' ? 'danger' : 'success'}>
                {w.status === 'Voided' ? 'Đã hủy' : w.isExpired ? 'Hết hạn' : 'Còn hiệu lực'}
              </Badge>
            </div>

            <div className={styles.cardBody}>
              <div className={styles.metaText}>
                <span>📅</span>
                <span>
                  Thời hạn bảo hành: <strong>{new Date(w.startDate).toLocaleDateString('vi-VN')}</strong> đến{' '}
                  <strong>{new Date(w.endDate).toLocaleDateString('vi-VN')}</strong>
                </span>
              </div>
            </div>

            <div className={styles.cardActions}>
              <Button variant="secondary" size="sm" onClick={() => navigate(`/tickets/${w.ticketId}`)}>
                Xem phiếu gốc
              </Button>
              {!w.isExpired && w.status !== 'Voided' && (
                <Button size="sm" onClick={() => setClaimingTicket(w)}>
                  🛡️ Yêu cầu bảo hành
                </Button>
              )}
            </div>
          </div>
        ))}
      </div>

      <WarrantyClaimModal
        isOpen={!!claimingTicket}
        ticketId={claimingTicket?.ticketId ?? ''}
        onClose={() => setClaimingTicket(null)}
        onDone={() => { setClaimingTicket(null); fetchData(); }}
      />
    </div>
  );
}