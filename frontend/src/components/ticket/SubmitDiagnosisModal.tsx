import { useEffect, useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { diagnosisService } from '../../services/diagnosisService';
import { partService } from '../../services/partService';
import { contentService } from '../../services/contentService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { DeviceType, DiagnosisSymptom } from '../../types/diagnosis.types';
import type { Part } from '../../types/inventory.types';
import type { ServiceItem } from '../../types/content.types';

interface Props {
  isOpen: boolean;
  ticketId: string;
  deviceType: DeviceType;
  onClose: () => void;
  onDone: () => void;
}

export default function SubmitDiagnosisModal({
  isOpen,
  ticketId,
  deviceType,
  onClose,
  onDone,
}: Props) {
  const [symptoms, setSymptoms] = useState<DiagnosisSymptom[]>([]);
  const [parts, setParts] = useState<Part[]>([]);
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [selectedSymptomIds, setSelectedSymptomIds] = useState<string[]>([]);
  const [selectedPartIds, setSelectedPartIds] = useState<string[]>([]);
  const [selectedServiceIds, setSelectedServiceIds] = useState<string[]>([]);
  const [other, setOther] = useState('');
  const [rootCause, setRootCause] = useState('');
  const [technicalNote, setTechnicalNote] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoadingOptions, setIsLoadingOptions] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const { showSuccess } = useToast();

  useEffect(() => {
    if (!isOpen) return;
    setErrorMessage(null);
    setIsLoadingOptions(true);
    Promise.all([
      diagnosisService.list(deviceType),
      partService.list({ page: 1, pageSize: 200 }),
      contentService.getPublicServices(),
    ])
      .then(([symptomItems, partResponse, serviceResponse]) => {
        setSymptoms(symptomItems);
        setParts(partResponse.items.filter((part) => part.isActive && part.quantityOnHand > 0));
        setServices(serviceResponse.items.filter((service) => service.isActive));
      })
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoadingOptions(false));
  }, [deviceType, isOpen]);

  const toggle = (
    id: string,
    selected: string[],
    setSelected: (value: string[]) => void
  ) => {
    setSelected(selected.includes(id) ? selected.filter((item) => item !== id) : [...selected, id]);
  };

  const handleSubmit = async () => {
    if (selectedSymptomIds.length === 0 && !other.trim()) {
      setErrorMessage('Vui lòng chọn ít nhất một triệu chứng hoặc nhập nội dung khác.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await ticketService.submitDiagnosis(ticketId, {
        diagnosisResult: '',
        diagnosisSymptomIds: selectedSymptomIds,
        diagnosisResultOther: other.trim() || undefined,
        rootCause: rootCause.trim() || undefined,
        requiredPartIds: selectedPartIds,
        recommendedServiceIds: selectedServiceIds,
        technicalNote: technicalNote.trim() || undefined,
      });
      showSuccess('Đã ghi nhận kết quả chẩn đoán.');
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const checklist = (
    title: string,
    items: { id: string; label: string }[],
    selected: string[],
    setSelected: (value: string[]) => void
  ) => (
    <fieldset style={{ border: '1px solid var(--color-border)', borderRadius: 6, padding: 12 }}>
      <legend style={{ fontSize: 13, fontWeight: 600 }}>{title}</legend>
      {items.length === 0 ? (
        <small style={{ color: 'var(--color-text-muted)' }}>Chưa có dữ liệu phù hợp.</small>
      ) : (
        <div style={{ display: 'grid', gap: 8, maxHeight: 180, overflowY: 'auto' }}>
          {items.map((item) => (
            <label key={item.id} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input
                type="checkbox"
                checked={selected.includes(item.id)}
                onChange={() => toggle(item.id, selected, setSelected)}
              />
              <span>{item.label}</span>
            </label>
          ))}
        </div>
      )}
    </fieldset>
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Chẩn đoán thiết bị"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>Hủy</Button>
          <Button onClick={handleSubmit} isLoading={isSaving} disabled={isLoadingOptions}>
            Lưu chẩn đoán
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {isLoadingOptions && <p>Đang tải danh mục chẩn đoán, kho và dịch vụ...</p>}
      {checklist(
        'Triệu chứng chẩn đoán *',
        symptoms.map((item) => ({ id: item.id, label: item.name })),
        selectedSymptomIds,
        setSelectedSymptomIds
      )}
      <label style={{ display: 'block', margin: '12px 0' }}>
        <span style={{ fontSize: 13, fontWeight: 500 }}>Khác</span>
        <textarea
          rows={2}
          value={other}
          onChange={(event) => setOther(event.target.value)}
          style={{ width: '100%', padding: 10, marginTop: 4, border: '1px solid var(--color-border)', borderRadius: 4 }}
          placeholder="Nhập triệu chứng chưa có trong danh mục"
        />
      </label>
      {checklist(
        'Linh kiện hiện có trong kho (chỉ chọn, không trừ kho)',
        parts.map((part) => ({ id: part.id, label: `${part.name} (${part.sku}) · tồn ${part.quantityOnHand}` })),
        selectedPartIds,
        setSelectedPartIds
      )}
      {checklist(
        'Dịch vụ đề xuất',
        services.map((service) => ({ id: service.id, label: service.name })),
        selectedServiceIds,
        setSelectedServiceIds
      )}
      <label style={{ display: 'block', marginTop: 12 }}>
        <span style={{ fontSize: 13, fontWeight: 500 }}>Nguyên nhân gốc</span>
        <textarea rows={2} value={rootCause} onChange={(event) => setRootCause(event.target.value)}
          style={{ width: '100%', padding: 10, marginTop: 4, border: '1px solid var(--color-border)', borderRadius: 4 }} />
      </label>
      <label style={{ display: 'block', marginTop: 12 }}>
        <span style={{ fontSize: 13, fontWeight: 500 }}>Ghi chú kỹ thuật</span>
        <textarea rows={2} value={technicalNote} onChange={(event) => setTechnicalNote(event.target.value)}
          style={{ width: '100%', padding: 10, marginTop: 4, border: '1px solid var(--color-border)', borderRadius: 4 }} />
      </label>
    </Modal>
  );
}
