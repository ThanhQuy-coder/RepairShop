import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { warrantyService } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import type { MyWarrantyItem } from '../../types/warranty.types';
import { Loading, ErrorMessage, EmptyState, Badge, Button } from '../../components/common';
import WarrantyClaimModal from '../../components/warranty/WarrantyClaimModal';
import styles from './MyWarrantyPage.module.css';

function getWarrantyRemainingInfo(startDateStr: string, endDateStr: string) {
  const start = new Date(startDateStr).getTime();
  const end = new Date(endDateStr).getTime();
  const now = Date.now();
  const totalMs = Math.max(1, end - start);
  const remainingMs = end - now;
  const remainingDays = Math.ceil(remainingMs / (1000 * 60 * 60 * 24));
  const percentRemaining = Math.max(0, Math.min(100, Math.round((remainingMs / totalMs) * 100)));
  return { remainingDays, percentRemaining };
}

export default function MyWarrantyPage() {
  const navigate = useNavigate();
  const [warranties, setWarranties] = useState<MyWarrantyItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [claimingTicket, setClaimingTicket] = useState<MyWarrantyItem | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'expired'>('all');

  const fetchData = () => {
    setIsLoading(true);
    setErrorMessage(null);
    warrantyService
      .getMyWarranties()
      .then(setWarranties)
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  };

  useEffect(fetchData, []);

  const stats = useMemo(() => {
    const active = warranties.filter((w) => !w.isExpired && w.status !== 'Voided').length;
    const expired = warranties.length - active;
    return { total: warranties.length, active, expired };
  }, [warranties]);

  const filteredWarranties = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return warranties.filter((w) => {
      const isActive = !w.isExpired && w.status !== 'Voided';
      if (statusFilter === 'active' && !isActive) return false;
      if (statusFilter === 'expired' && isActive) return false;
      if (q) {
        return (
          w.warrantyCode.toLowerCase().includes(q) ||
          w.deviceLabel.toLowerCase().includes(q) ||
          w.ticketCode.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [warranties, searchQuery, statusFilter]);

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} onRetry={fetchData} />;

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <span className={styles.eyebrow}>TRUNG TÂM BẢO HÀNH ĐIỆN TỬ — FIXORA</span>
        <h1 className={styles.title}>Bảo hành của tôi</h1>
        <p className={styles.subtitle}>
          Tra cứu chứng nhận bảo hành điện tử, thời hạn còn lại và gửi yêu cầu tái khám bảo hành trực tuyến.
        </p>
      </header>

      {warranties.length === 0 ? (
        <EmptyState message="Bạn chưa có chứng nhận bảo hành nào tại Fixora." />
      ) : (
        <>
          <div className={styles.kpiGrid}>
            <button
              type="button"
              className={`${styles.kpiCard} ${statusFilter === 'all' ? styles.kpiCardActive : ''}`}
              onClick={() => setStatusFilter('all')}
            >
              <strong>{stats.total}</strong>
              <span>Tổng số phiếu bảo hành</span>
            </button>
            <button
              type="button"
              className={`${styles.kpiCard} ${
                statusFilter === 'active' ? styles.kpiCardActive : ''
              }`}
              onClick={() => setStatusFilter('active')}
            >
              <strong>{stats.active}</strong>
              <span>Đang còn hiệu lực</span>
            </button>
            <button
              type="button"
              className={`${styles.kpiCard} ${
                statusFilter === 'expired' ? styles.kpiCardActive : ''
              }`}
              onClick={() => setStatusFilter('expired')}
            >
              <strong>{stats.expired}</strong>
              <span>Đã hết hạn / Đã đóng</span>
            </button>
          </div>

          <div className={styles.filterToolbar}>
            <div className={styles.searchBox}>
              <span>🔎</span>
              <input
                type="text"
                placeholder="Tìm theo mã bảo hành, mã phiếu sửa gốc hoặc tên thiết bị..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>

          {filteredWarranties.length === 0 ? (
            <EmptyState message="Không tìm thấy chứng nhận bảo hành phù hợp với bộ lọc." />
          ) : (
            <div className={styles.list}>
              {filteredWarranties.map((w) => {
                const isActive = !w.isExpired && w.status !== 'Voided';
                const { remainingDays, percentRemaining } = getWarrantyRemainingInfo(
                  w.startDate,
                  w.endDate
                );

                return (
                  <div key={w.warrantyCode} className={styles.card}>
                    <div className={styles.cardHeader}>
                      <div className={styles.deviceInfo}>
                        <div className={styles.shieldIcon}>🛡️</div>
                        <div>
                          <div className={styles.deviceTitle}>{w.deviceLabel}</div>
                          <div className={styles.codeBadgeRow}>
                            <span className={styles.warrantyCodeTag}>Mã BH: {w.warrantyCode}</span>
                            <span className={styles.warrantyCodeTag}>
                              Phiếu gốc: {w.ticketCode}
                            </span>
                          </div>
                        </div>
                      </div>
                      <Badge variant={!isActive ? 'danger' : 'success'}>
                        {w.status === 'Voided'
                          ? 'Đã hủy hiệu lực'
                          : w.isExpired
                            ? 'Đã hết hạn'
                            : 'Đang bảo hành'}
                      </Badge>
                    </div>

                    <div className={styles.cardBody}>
                      <div className={styles.metaText}>
                        <span>📅</span>
                        <span>
                          Thời hạn bảo hành:{' '}
                          <strong>{new Date(w.startDate).toLocaleDateString('vi-VN')}</strong> đến{' '}
                          <strong>{new Date(w.endDate).toLocaleDateString('vi-VN')}</strong>
                        </span>
                      </div>

                      <div className={styles.warrantyProgressWrap}>
                        <div className={styles.warrantyProgressTop}>
                          <span>
                            {isActive && remainingDays >= 0
                              ? `Còn ${remainingDays} ngày hiệu lực bảo hành`
                              : 'Thời hạn bảo hành đã kết thúc'}
                          </span>
                          <strong>{isActive ? `${percentRemaining}%` : '0%'}</strong>
                        </div>
                        <div className={styles.warrantyProgressTrack}>
                          <div
                            className={styles.warrantyProgressFill}
                            data-expired={!isActive}
                            style={{ width: `${isActive ? percentRemaining : 100}%` }}
                          />
                        </div>
                      </div>
                    </div>

                    <div className={styles.cardActions}>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => navigate(`/tickets/${w.ticketId}`)}
                      >
                        Xem phiếu sửa gốc ({w.ticketCode})
                      </Button>
                      {isActive && (
                        <Button size="sm" onClick={() => setClaimingTicket(w)}>
                          🛡️ Đặt lịch yêu cầu bảo hành
                        </Button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}

      <WarrantyClaimModal
        isOpen={!!claimingTicket}
        ticketId={claimingTicket?.ticketId ?? ''}
        onClose={() => setClaimingTicket(null)}
        onDone={() => {
          setClaimingTicket(null);
          fetchData();
        }}
      />
    </div>
  );
}
