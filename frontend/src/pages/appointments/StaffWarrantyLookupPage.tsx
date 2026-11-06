import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Badge,
  Button,
  ErrorMessage,
  Input,
  Loading,
  Modal,
  Table,
  type TableColumn,
} from '../../components/common';
import {
  warrantyService,
  type StaffWarrantyLookup,
  type WarrantyDetail,
} from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';
import styles from './StaffWarrantyLookupPage.module.css';

function getDaysRemaining(endDateStr: string): number {
  const end = new Date(endDateStr);
  const now = new Date();
  const diffMs = end.getTime() - now.getTime();
  return Math.ceil(diffMs / (1000 * 60 * 60 * 24));
}

export default function StaffWarrantyLookupPage() {
  const navigate = useNavigate();
  const [phone, setPhone] = useState('');
  const [warrantyCode, setWarrantyCode] = useState('');
  const [rows, setRows] = useState<StaffWarrantyLookup[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedWarranty, setSelectedWarranty] = useState<StaffWarrantyLookup | null>(null);
  const [warrantyDetail, setWarrantyDetail] = useState<WarrantyDetail | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);

  const performSearch = async (params?: { phone?: string; warrantyCode?: string }) => {
    setIsSearching(true);
    setError(null);
    try {
      const data = await warrantyService.searchForStaff(
        params ?? {
          phone: phone.trim() || undefined,
          warrantyCode: warrantyCode.trim() || undefined,
        }
      );
      setRows(data);
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsSearching(false);
    }
  };

  useEffect(() => {
    // Tải danh sách bảo hành gần đây khi mở trang để lễ tân dễ tra cứu
    warrantyService
      .searchForStaff({})
      .then(setRows)
      .catch(() => {
        // Ignore if backend requires filter parameters
      });
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void performSearch();
  };

  const handleClear = () => {
    setPhone('');
    setWarrantyCode('');
    void performSearch({});
  };

  const openDetail = async (row: StaffWarrantyLookup) => {
    setSelectedWarranty(row);
    setWarrantyDetail(null);
    setIsLoadingDetail(true);
    try {
      setWarrantyDetail(await warrantyService.getByTicket(row.ticketId));
    } catch (err) {
      setError(extractApiError(err).message);
    } finally {
      setIsLoadingDetail(false);
    }
  };

  const stats = useMemo(() => {
    const active = rows.filter(
      (r) => r.status === 'Active' && getDaysRemaining(r.endDate) >= 0
    ).length;
    const expired = rows.length - active;
    return { total: rows.length, active, expired };
  }, [rows]);

  const columns: TableColumn<StaffWarrantyLookup>[] = [
    {
      key: 'code',
      header: 'Mã bảo hành',
      render: (row) => <span className={styles.codeBadge}>{row.warrantyCode}</span>,
    },
    {
      key: 'customer',
      header: 'Khách hàng',
      render: (row) => (
        <div className={styles.customerCell}>
          <strong>{row.customerName}</strong>
          <span>📞 {row.customerPhone}</span>
        </div>
      ),
    },
    {
      key: 'device',
      header: 'Thiết bị bảo hành',
      render: (row) => <strong>{row.deviceLabel}</strong>,
    },
    {
      key: 'ticket',
      header: 'Phiếu sửa gốc',
      render: (row) => (
        <button
          type="button"
          className={styles.ticketLinkBtn}
          onClick={(e) => {
            e.stopPropagation();
            navigate(`/tickets/${row.ticketId}`);
          }}
        >
          {row.ticketCode} ↗
        </button>
      ),
    },
    {
      key: 'period',
      header: 'Thời hạn bảo hành',
      render: (row) => {
        const days = getDaysRemaining(row.endDate);
        return (
          <div className={styles.periodCell}>
            <span>
              {new Date(row.startDate).toLocaleDateString('vi-VN')} →{' '}
              {new Date(row.endDate).toLocaleDateString('vi-VN')}
            </span>
            {days >= 0 && row.status === 'Active' ? (
              <span className={styles.daysRemaining}>Còn {days} ngày hiệu lực</span>
            ) : (
              <span className={styles.daysExpired}>Đã hết hạn</span>
            )}
          </div>
        );
      },
    },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (row) => {
        const isStillValid = row.status === 'Active' && getDaysRemaining(row.endDate) >= 0;
        return (
          <Badge variant={isStillValid ? 'success' : 'default'}>
            {isStillValid ? 'Còn hiệu lực' : 'Hết hạn'}
          </Badge>
        );
      },
    },
    {
      key: 'actions',
      header: 'Thao tác',
      render: (row) => (
        <div className={styles.rowActions} onClick={(e) => e.stopPropagation()}>
          <Button size="sm" variant="secondary" onClick={() => void openDetail(row)}>
            Chi tiết BH
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>NGHIỆP VỤ HẬU MÃI — FIXORA</span>
          <h1 className={styles.title}>Tra cứu Bảo hành Thiết bị</h1>
          <p className={styles.subtitle}>
            Kiểm tra nhanh thời hạn, điều khoản bảo hành và phiếu sửa chữa gốc bằng số điện thoại khách hàng hoặc mã bảo hành.
          </p>
        </div>
        <Button variant="secondary" onClick={() => navigate('/tickets/create')}>
          + Tiếp nhận thiết bị mới
        </Button>
      </div>

      <div className={styles.kpiStrip}>
        <div className={styles.kpiBox}>
          <span className={styles.kpiIcon}>🛡️</span>
          <div>
            <strong>{stats.total}</strong>
            <span>Phiếu bảo hành hiển thị</span>
          </div>
        </div>
        <div className={styles.kpiBox}>
          <span className={styles.kpiIcon}>✅</span>
          <div>
            <strong>{stats.active}</strong>
            <span>Đang trong hạn bảo hành</span>
          </div>
        </div>
        <div className={styles.kpiBox}>
          <span className={styles.kpiIcon}>⌛</span>
          <div>
            <strong>{stats.expired}</strong>
            <span>Đã hết hạn / Đóng</span>
          </div>
        </div>
      </div>

      {error && <ErrorMessage message={error} />}

      <div className={styles.searchCard}>
        <div className={styles.searchCardHeader}>
          <h2>Bộ lọc tra cứu nhanh</h2>
        </div>
        <form onSubmit={handleSubmit} className={styles.searchForm}>
          <Input
            label="Số điện thoại khách hàng"
            placeholder="Nhập SĐT (VD: 0901234567)..."
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
          <Input
            label="Mã phiếu bảo hành"
            placeholder="Nhập mã BH (VD: WR-2026-...)..."
            value={warrantyCode}
            onChange={(e) => setWarrantyCode(e.target.value)}
          />
          <div className={styles.searchActions}>
            <Button type="submit" isLoading={isSearching}>
              🔍 Tra cứu
            </Button>
            {(phone || warrantyCode) && (
              <Button type="button" variant="secondary" onClick={handleClear}>
                Xóa lọc
              </Button>
            )}
          </div>
        </form>
      </div>

      <Table
        columns={columns}
        data={rows}
        keyExtractor={(row) => row.warrantyCode}
        isLoading={isSearching}
        emptyMessage="Không tìm thấy phiếu bảo hành nào khớp với thông tin tra cứu."
        onRowClick={(row) => void openDetail(row)}
      />

      <Modal
        isOpen={!!selectedWarranty}
        onClose={() => setSelectedWarranty(null)}
        title="Chi tiết chứng nhận bảo hành Fixora"
      >
        {isLoadingDetail && <Loading />}
        {!isLoadingDetail && warrantyDetail && selectedWarranty && (
          <div className={styles.detailContainer}>
            <div className={styles.detailBanner}>
              <div>
                <span className={styles.eyebrow}>MÃ BẢO HÀNH ĐIỆN TỬ</span>
                <div className={styles.detailBannerCode}>{warrantyDetail.warrantyCode}</div>
              </div>
              <Badge
                variant={
                  !warrantyDetail.isExpired && warrantyDetail.status === 'Active'
                    ? 'success'
                    : 'default'
                }
              >
                {warrantyDetail.isExpired
                  ? 'Đã hết hạn'
                  : warrantyDetail.status === 'Active'
                    ? 'Đang hiệu lực'
                    : 'Đã hủy'}
              </Badge>
            </div>

            <div className={styles.detailGrid}>
              <div className={styles.detailBox}>
                <label>Khách hàng</label>
                <strong>{selectedWarranty.customerName}</strong>
                <span>📞 {selectedWarranty.customerPhone}</span>
              </div>
              <div className={styles.detailBox}>
                <label>Thiết bị được bảo hành</label>
                <strong>{selectedWarranty.deviceLabel}</strong>
                <span>Phiếu sửa gốc: {selectedWarranty.ticketCode}</span>
              </div>
              <div className={styles.detailBox}>
                <label>Ngày kích hoạt</label>
                <strong>
                  {new Date(warrantyDetail.startDate).toLocaleDateString('vi-VN')}
                </strong>
              </div>
              <div className={styles.detailBox}>
                <label>Ngày hết hạn</label>
                <strong>
                  {new Date(warrantyDetail.endDate).toLocaleDateString('vi-VN')}
                </strong>
              </div>
            </div>

            <div className={styles.termsBox}>
              <label>Điều khoản &amp; Phạm vi bảo hành</label>
              <p>
                {warrantyDetail.terms ||
                  'Bảo hành tiêu chuẩn Fixora cho linh kiện thay thế và hạng mục đã sửa chữa. Không áp dụng cho rơi vỡ, vào nước hoặc can thiệp bên thứ ba.'}
              </p>
            </div>

            <div className={styles.modalFooter}>
              <Button
                variant="secondary"
                onClick={() => navigate(`/tickets/${selectedWarranty.ticketId}`)}
              >
                Mở phiếu sửa gốc ({selectedWarranty.ticketCode})
              </Button>
              <Button onClick={() => setSelectedWarranty(null)}>Đóng</Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
