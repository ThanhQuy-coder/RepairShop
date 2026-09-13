import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { deviceService } from '../../services/deviceService';
import { customerService } from '../../services/customerService';
import { extractApiError } from '../../utils/apiError';
import type { Device } from '../../types/device.types';
import type { Customer } from '../../types/customer.types';
import { Button, Table, ErrorMessage, type TableColumn } from '../../components/common';
import CustomerPicker from '../../components/customer/CustomerPicker';
import DeviceFormModal from '../../components/device/DeviceFormModal';
import styles from './DevicesPage.module.css';

export default function DevicesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const customerId = searchParams.get('customerId');

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);

  useEffect(() => {
    if (!customerId) {
      setDevices([]);
      setCustomer(null);
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
    load();
  }, [customerId]);

  const columns: TableColumn<Device>[] = [
    { key: 'brand', header: 'Hãng / Model', render: (d) => `${d.brand} ${d.model}` },
    { key: 'deviceType', header: 'Loại', render: (d) => d.deviceType },
    { key: 'serialNumber', header: 'Serial/IMEI', render: (d) => d.serialNumber ?? '—' },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>QUẢN LÝ THIẾT BỊ</span>
          <h1 className={styles.title}>Danh mục thiết bị</h1>
          <p className={styles.subtitle}>Quản lý thông tin điện thoại, máy tính, tablet theo từng khách hàng.</p>
        </div>
        {customer && (
          <Button size="md" onClick={() => setIsFormOpen(true)}>
            + Thêm thiết bị
          </Button>
        )}
      </div>

      {!customerId ? (
        <div className={styles.pickerBox}>
          <div className={styles.pickerIcon}>🔍</div>
          <h3>Chọn khách hàng để quản lý thiết bị</h3>
          <p>Tìm kiếm theo tên hoặc số điện thoại để xem danh sách máy móc của khách:</p>
          <div className={styles.pickerInputWrap}>
            <CustomerPicker onSelect={(c) => setSearchParams({ customerId: c.id })} />
          </div>
        </div>
      ) : (
        <>
          {customer && (
            <div className={styles.customerCard}>
              <div>
                <span className={styles.customerLabel}>Khách hàng đang chọn</span>
                <p className={styles.customerName}>
                  <strong>{customer.fullName}</strong> — {customer.phone}
                </p>
              </div>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSearchParams({})}
              >
                Đổi khách hàng
              </Button>
            </div>
          )}


          {errorMessage && <ErrorMessage message={errorMessage} />}

          <Table
            columns={columns}
            data={devices}
            keyExtractor={(d) => d.id}
            isLoading={isLoading}
            emptyMessage="Khách hàng chưa có thiết bị nào."
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
