import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { deviceService } from '../../services/deviceService';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import {
  DEVICE_TYPE_LABELS,
  DEVICE_TYPE_OPTIONS,
  type Device,
} from '../../types/device.types';
import type { Customer } from '../../types/customer.types';
import {
  Badge,
  Button,
  Table,
  ErrorMessage,
  Loading,
  type TableColumn,
} from '../../components/common';
import CustomerPicker from '../../components/customer/CustomerPicker';
import DeviceFormModal from '../../components/device/DeviceFormModal';
import styles from './DevicesPage.module.css';

const DEVICE_ICONS: Record<string, string> = {
  Phone: '📱',
  Laptop: '💻',
  Tablet: '📟',
  Smartwatch: '⌚',
  Electronics: '🔌',
};

export default function DevicesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const customerId = searchParams.get('customerId');

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [recentCustomers, setRecentCustomers] = useState<Customer[]>([]);
  const [isLoadingRecent, setIsLoadingRecent] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const [deviceSearch, setDeviceSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');

  useEffect(() => {
    if (customerId) return;
    let active = true;
    setIsLoadingRecent(true);
    customerService
      .list({ page: 1, pageSize: 8 })
      .then((res) => {
        if (active) setRecentCustomers(res.items);
      })
      .catch(() => {
        // Ignore silent error on quick suggestions
      })
      .finally(() => {
        if (active) setIsLoadingRecent(false);
      });
    return () => {
      active = false;
    };
  }, [customerId]);

  useEffect(() => {
    if (!customerId) {
      setDevices([]);
      setCustomer(null);
      setDeviceSearch('');
      setTypeFilter('');
      return;
    }

    const load = async () => {
      setIsLoading(true);
      setErrorMessage(null);
      try {
        const [customerData, deviceList] = await Promise.all([
          customerService.getById(customerId),
          deviceService.getByCustomerId(customerId),
        ]);
        setCustomer(customerData);
        setDevices(deviceList);
      } catch (err) {
        setErrorMessage(extractApiError(err).message);
      } finally {
        setIsLoading(false);
      }
    };
    void load();
  }, [customerId]);

  const filteredDevices = useMemo(() => {
    const q = deviceSearch.trim().toLowerCase();
    return devices.filter((d) => {
      if (typeFilter && d.deviceType !== typeFilter) return false;
      if (!q) return true;
      return (
        `${d.brand} ${d.model}`.toLowerCase().includes(q) ||
        (d.serialNumber ?? '').toLowerCase().includes(q)
      );
    });
  }, [devices, deviceSearch, typeFilter]);

  const columns: TableColumn<Device>[] = [
    {
      key: 'brand',
      header: 'Thiết bị (Hãng / Model)',
      render: (d) => (
        <div className={styles.deviceCell}>
          <span className={styles.deviceEmoji}>{DEVICE_ICONS[d.deviceType] ?? '📱'}</span>
          <div>
            <strong className={styles.deviceTitle}>
              {d.brand} {d.model}
            </strong>
            <span className={styles.deviceSub}>Mã TB: {d.id.slice(0, 8).toUpperCase()}</span>
          </div>
        </div>
      ),
    },
    {
      key: 'deviceType',
      header: 'Phân loại',
      render: (d) => (
        <Badge variant="info">{DEVICE_TYPE_LABELS[d.deviceType] || d.deviceType}</Badge>
      ),
    },
    {
      key: 'serialNumber',
      header: 'Serial / IMEI',
      render: (d) =>
        d.serialNumber ? (
          <code className={styles.serialBadge}>{d.serialNumber}</code>
        ) : (
          <span className={styles.mutedText}>Chưa cập nhật</span>
        ),
    },
    {
      key: 'createdAt',
      header: 'Ngày lưu hồ sơ',
      render: (d) =>
        d.createdAt ? new Date(d.createdAt).toLocaleDateString('vi-VN') : '—',
    },
    {
      key: 'actions',
      header: 'Thao tác',
      render: (d) => (
        <div
          className={styles.rowActions}
          onClick={(e) => e.stopPropagation()}
        >
          <Button
            size="sm"
            variant="secondary"
            onClick={() => navigate(`/devices/${d.id}`)}
          >
            Xem lịch sử
          </Button>
          <Button
            size="sm"
            onClick={() => navigate('/tickets/create')}
          >
            + Tạo phiếu sửa
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>HỒ SƠ THIẾT BỊ KHÁCH HÀNG</span>
          <h1 className={styles.title}>Quản lý Thiết bị &amp; Lịch sử máy</h1>
          <p className={styles.subtitle}>
            Tra cứu thiết bị theo khách hàng, kiểm tra số Serial/IMEI và lịch sử sửa chữa tại Fixora.
          </p>
        </div>
        <div className={styles.headerButtons}>
          {customer && (
            <>
              <Button
                variant="secondary"
                size="md"
                onClick={() => navigate(`/customers/${customer.id}`)}
              >
                Hồ sơ khách hàng
              </Button>
              <Button size="md" onClick={() => setIsFormOpen(true)}>
                + Thêm thiết bị mới
              </Button>
            </>
          )}
        </div>
      </div>

      {!customerId ? (
        <div className={styles.lookupGrid}>
          <div className={styles.pickerBox}>
            <div className={styles.pickerIcon}>🔍</div>
            <h3>Tra cứu khách hàng để quản lý thiết bị</h3>
            <p>
              Nhập họ tên hoặc số điện thoại khách hàng để xem danh sách thiết bị đã đăng ký hoặc thêm máy mới:
            </p>
            <div className={styles.pickerInputWrap}>
              <CustomerPicker
                onSelect={(c) => setSearchParams({ customerId: c.id })}
                placeholder="Nhập tên hoặc SĐT khách hàng (VD: 090...)"
              />
            </div>
            <div className={styles.pickerTips}>
              <span>💡 Mẹo nhanh:</span> Bạn cũng có thể chọn nhanh một khách hàng từ danh sách bên phải.
            </div>
          </div>

          <div className={styles.recentCard}>
            <div className={styles.recentHeader}>
              <div>
                <span className={styles.sectionKicker}>CHỌN NHANH</span>
                <h3>Khách hàng gần đây</h3>
              </div>
              <Button variant="ghost" size="sm" onClick={() => navigate('/customers')}>
                Tất cả khách hàng →
              </Button>
            </div>
            {isLoadingRecent ? (
              <Loading />
            ) : recentCustomers.length === 0 ? (
              <p className={styles.mutedText}>Chưa có dữ liệu khách hàng.</p>
            ) : (
              <div className={styles.recentList}>
                {recentCustomers.map((c) => (
                  <button
                    key={c.id}
                    type="button"
                    className={styles.recentCustomerBtn}
                    onClick={() => setSearchParams({ customerId: c.id })}
                  >
                    <span className={styles.customerAvatar}>
                      {c.fullName.trim().charAt(0).toUpperCase()}
                    </span>
                    <div className={styles.recentCustomerInfo}>
                      <strong>{c.fullName}</strong>
                      <span>
                        📞 {c.phone} {c.email ? `· ✉️ ${c.email}` : ''}
                      </span>
                    </div>
                    <span className={styles.selectCue}>Xem thiết bị →</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      ) : (
        <>
          {customer && (
            <div className={styles.customerBanner}>
              <div className={styles.customerBannerLeft}>
                <div className={styles.bannerAvatar}>
                  {customer.fullName.trim().charAt(0).toUpperCase()}
                </div>
                <div>
                  <span className={styles.customerLabel}>KHÁCH HÀNG ĐANG CHỌN</span>
                  <h2 className={styles.customerName}>{customer.fullName}</h2>
                  <div className={styles.customerMetaRow}>
                    <span>📞 {customer.phone}</span>
                    {customer.email && <span>✉️ {customer.email}</span>}
                    {customer.address && <span>📍 {customer.address}</span>}
                  </div>
                </div>
              </div>

              <div className={styles.customerBannerRight}>
                <div className={styles.deviceCountBox}>
                  <strong>{devices.length}</strong>
                  <span>Thiết bị đã lưu</span>
                </div>
                <Button variant="secondary" size="sm" onClick={() => setSearchParams({})}>
                  ⇄ Đổi khách hàng khác
                </Button>
              </div>
            </div>
          )}

          {errorMessage && <ErrorMessage message={errorMessage} />}

          <div className={styles.filterToolbar}>
            <div className={styles.searchField}>
              <span>🔎</span>
              <input
                type="text"
                placeholder="Tìm theo hãng, model hoặc số Serial/IMEI..."
                value={deviceSearch}
                onChange={(e) => setDeviceSearch(e.target.value)}
              />
            </div>
            <div className={styles.typePills}>
              <button
                type="button"
                className={`${styles.typePill} ${typeFilter === '' ? styles.typePillActive : ''}`}
                onClick={() => setTypeFilter('')}
              >
                Tất cả ({devices.length})
              </button>
              {DEVICE_TYPE_OPTIONS.map((opt) => {
                const count = devices.filter((d) => d.deviceType === opt.value).length;
                if (count === 0 && typeFilter !== opt.value) return null;
                return (
                  <button
                    key={opt.value}
                    type="button"
                    className={`${styles.typePill} ${
                      typeFilter === opt.value ? styles.typePillActive : ''
                    }`}
                    onClick={() => setTypeFilter(opt.value)}
                  >
                    {DEVICE_TYPE_LABELS[opt.value]} ({count})
                  </button>
                );
              })}
            </div>
          </div>

          <Table
            columns={columns}
            data={filteredDevices}
            keyExtractor={(d) => d.id}
            isLoading={isLoading}
            emptyMessage="Khách hàng chưa có thiết bị nào phù hợp. Nhấn '+ Thêm thiết bị mới' để đăng ký."
            onRowClick={(d) => navigate(`/devices/${d.id}`)}
          />
        </>
      )}

      {customer && (
        <DeviceFormModal
          isOpen={isFormOpen}
          customerId={customer.id}
          onClose={() => setIsFormOpen(false)}
          onSaved={(d) => setDevices((prev) => [...prev, d])}
        />
      )}
    </div>
  );
}
