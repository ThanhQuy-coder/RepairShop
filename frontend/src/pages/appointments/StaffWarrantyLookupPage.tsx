import { useState } from 'react';
import { Button, ErrorMessage, Input, Table, type TableColumn } from '../../components/common';
import { warrantyService, type StaffWarrantyLookup } from '../../services/warrantyService';
import { extractApiError } from '../../utils/apiError';

export default function StaffWarrantyLookupPage() {
  const [phone, setPhone] = useState('');
  const [warrantyCode, setWarrantyCode] = useState('');
  const [rows, setRows] = useState<StaffWarrantyLookup[]>([]);
  const [error, setError] = useState<string | null>(null);
  const columns: TableColumn<StaffWarrantyLookup>[] = [
    { key: 'code', header: 'Mã bảo hành', render: (row) => row.warrantyCode },
    { key: 'customer', header: 'Khách hàng', render: (row) => `${row.customerName} - ${row.customerPhone}` },
    { key: 'device', header: 'Thiết bị', render: (row) => row.deviceLabel },
    { key: 'ticket', header: 'Ticket gốc', render: (row) => row.ticketCode },
    { key: 'period', header: 'Thời hạn', render: (row) => `${row.startDate} - ${row.endDate}` },
  ];
  const search = async () => {
    try {
      setError(null);
      setRows(await warrantyService.searchForStaff({ phone: phone || undefined, warrantyCode: warrantyCode || undefined }));
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };
  return (
    <div style={{ maxWidth: 1200, margin: '0 auto' }}>
      <h1>Tra cứu bảo hành</h1>
      <p>Tra cứu nhanh bằng số điện thoại hoặc mã bảo hành trước khi tiếp nhận thiết bị.</p>
      {error && <ErrorMessage message={error} />}
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'end', margin: '20px 0' }}>
        <Input label="Số điện thoại" value={phone} onChange={(e) => setPhone(e.target.value)} />
        <Input label="Mã bảo hành" value={warrantyCode} onChange={(e) => setWarrantyCode(e.target.value)} />
        <Button onClick={() => void search()}>Tra cứu</Button>
      </div>
      <Table columns={columns} data={rows} keyExtractor={(row) => row.warrantyCode} emptyMessage="Chưa có kết quả." />
    </div>
  );
}
