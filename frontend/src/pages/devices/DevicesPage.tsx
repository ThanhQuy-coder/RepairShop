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
    <div>
      <div className={styles.header}>
        <h2>Thiết bị</h2>
        {customer && <Button onClick={() => setIsFormOpen(true)}>+ Thêm thiết bị</Button>}
      </div>

      {!customerId ? (
        <div className={styles.pickerBox}>
          <p>Chọn khách hàng để xem/quản lý thiết bị của họ:</p>
          <CustomerPicker onSelect={(c) => setSearchParams({ customerId: c.id })} />
        </div>
      ) : (
        <>
          {customer && (
            <p className={styles.customerInfo}>
              Khách hàng: <strong>{customer.fullName}</strong> ({customer.phone}) —{' '}
              <button className={styles.changeLink} onClick={() => setSearchParams({})}>
                Đổi khách hàng
              </button>
            </p>
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
