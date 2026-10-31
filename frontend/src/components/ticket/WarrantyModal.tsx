import { useEffect, useMemo, useState } from 'react';
import { Modal, Button, ErrorMessage, Loading } from '../common';
import { ticketService } from '../../services/ticketService';
import { warrantyPackageService } from '../../services/warrantyPackageService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { WarrantyPackage } from '../../types/warrantyPackage.types';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function WarrantyModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [packages, setPackages] = useState<WarrantyPackage[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();
  const selected = useMemo(() => packages.find((item) => item.id === selectedId), [packages, selectedId]);

  useEffect(() => {
    if (!isOpen) return;
    setIsLoading(true);
    setErrorMessage(null);
    warrantyPackageService.listActive()
      .then((items) => {
        setPackages(items);
        setSelectedId(items[0]?.id ?? '');
      })
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, [isOpen]);

  const handleSubmit = async () => {
    if (!selectedId) {
      setErrorMessage('Vui lòng chọn gói bảo hành.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.createWarranty(ticketId, selectedId);
      showSuccess('Đã tạo thông tin bảo hành.');
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Cấp gói bảo hành"
      footer={<><Button variant="secondary" onClick={onClose}>Hủy</Button><Button onClick={handleSubmit} isLoading={isSaving}>Cấp bảo hành</Button></>}>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {isLoading ? <Loading /> : packages.length === 0 ? <p>Chưa có gói bảo hành đang hoạt động.</p> : (
        <>
          <label style={{ fontSize: 13, fontWeight: 500, display: 'block', marginBottom: 8 }}>
            Gói bảo hành
            <select style={{ width: '100%', padding: 10, marginTop: 4 }} value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
              {packages.map((item) => <option key={item.id} value={item.id}>{item.name} — {item.durationMonths} tháng</option>)}
            </select>
          </label>
          {selected && <div style={{ marginTop: 12, padding: 12, background: 'var(--color-surface-subtle, #f8fafc)', borderRadius: 8 }}>
            <strong>{selected.name}</strong>
            {selected.description && <p>{selected.description}</p>}
            <p><b>Thời hạn:</b> {selected.durationMonths} tháng</p>
            <p><b>Điều khoản:</b> {selected.terms}</p>
            <small>Ngày bắt đầu là ngày bàn giao thực tế; ngày kết thúc được hệ thống tự tính.</small>
          </div>}
        </>
      )}
    </Modal>
  );
}
