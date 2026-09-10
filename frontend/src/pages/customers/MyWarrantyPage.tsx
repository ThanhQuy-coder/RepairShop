import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import type { MyWarrantyItem } from '../../types/warranty.types';
import { Loading, ErrorMessage, EmptyState, Badge, Button } from '../../components/common';
import WarrantyClaimModal from '../../components/warranty/WarrantyClaimModal';

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
    <div>
      <h2 style={{ marginBottom: 16 }}>Bảo hành của tôi</h2>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {warranties.map((w) => (
          <div key={w.warrantyCode} style={{ border: '1px solid var(--color-border)', borderRadius: 8, padding: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <strong>{w.deviceLabel}</strong>
              <Badge variant={w.isExpired || w.status === 'Voided' ? 'danger' : 'success'}>
                {w.status === 'Voided' ? 'Đã hủy' : w.isExpired ? 'Hết hạn' : 'Còn hiệu lực'}
              </Badge>
            </div>
            <p style={{ fontSize: 14, color: 'var(--color-text-muted)' }}>
              Bảo hành: {new Date(w.startDate).toLocaleDateString('vi-VN')} → {new Date(w.endDate).toLocaleDateString('vi-VN')}
            </p>
            <p style={{ fontSize: 14, color: 'var(--color-text-muted)' }}>Mã: {w.warrantyCode}</p>

            <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
              <Button variant="secondary" size="sm" onClick={() => navigate(`/tickets/${w.ticketId}`)}>
                Xem phiếu gốc
              </Button>
              {!w.isExpired && w.status !== 'Voided' && (
                <Button size="sm" onClick={() => setClaimingTicket(w)}>Yêu cầu bảo hành</Button>
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