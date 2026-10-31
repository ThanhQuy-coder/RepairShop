import { useEffect, useMemo, useState } from 'react';
import { Modal, Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { TicketDetail } from '../../types/ticket.types';
import type { InspectionItem, InspectionTemplate, InspectionType } from '../../types/inspection.types';

interface Props {
  isOpen: boolean;
  ticket: TicketDetail;
  onClose: () => void;
  onDone: () => void;
}

const functionalStatuses = ['Pass', 'Fail', 'NotApplicable', 'NotTested'];
const appearanceStatuses = ['Normal', 'Damaged', 'Unknown'];
const repairStatuses = ['Fixed', 'PartiallyFixed', 'NotFixed', 'UnableToVerify'];

export default function QaPassModal({ isOpen, ticket, onClose, onDone }: Props) {
  const [functional, setFunctional] = useState<InspectionItem[]>([]);
  const [appearance, setAppearance] = useState<InspectionItem[]>([]);
  const [verification, setVerification] = useState<InspectionItem[]>([]);
  const [appearanceFiles, setAppearanceFiles] = useState<Record<number, File | undefined>>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const { showSuccess } = useToast();

  useEffect(() => {
    if (!isOpen) return;
    setErrorMessage(null);
    setIsLoading(true);
    const repairItems = (ticket.diagnosisResult ?? '')
      .split(';')
      .map((item) => item.trim())
      .filter(Boolean)
      .map((item) => ({ itemName: item, status: 'Fixed', note: '' }));
    Promise.all([
      ticketService.getInspectionTemplates(ticket.id, 'Functional'),
      ticketService.getInspectionTemplates(ticket.id, 'Appearance'),
      ticketService.getInspection(ticket.id, 'Functional'),
      ticketService.getInspection(ticket.id, 'Appearance'),
      ticketService.getInspection(ticket.id, 'RepairVerification'),
    ]).then(([functionalTemplates, appearanceTemplates, savedFunctional, savedAppearance, savedVerification]) => {
      setFunctional(mergeItems(functionalTemplates, savedFunctional.items, 'NotTested'));
      setAppearance(mergeItems(appearanceTemplates, savedAppearance.items, 'Unknown'));
      setVerification(savedVerification.items.length ? savedVerification.items : repairItems);
    }).catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, [isOpen, ticket]);

  const update = (
    setter: React.Dispatch<React.SetStateAction<InspectionItem[]>>,
    index: number,
    patch: Partial<InspectionItem>
  ) => setter((items) => items.map((item, itemIndex) =>
    itemIndex === index ? { ...item, ...patch } : item));

  const validate = (items: InspectionItem[], type: InspectionType) => items.every((item) => {
    const needsNote = type === 'Functional'
      ? ['Fail', 'NotTested'].includes(item.status)
      : type === 'Appearance'
        ? ['Damaged', 'Unknown'].includes(item.status)
        : ['PartiallyFixed', 'NotFixed', 'UnableToVerify'].includes(item.status);
    return !needsNote || Boolean(item.note?.trim());
  });

  const handleSubmit = async () => {
    setErrorMessage(null);
    if (!validate(functional, 'Functional') || !validate(appearance, 'Appearance') ||
      !validate(verification, 'RepairVerification')) {
      setErrorMessage('Vui lòng bổ sung ghi chú cho các mục chưa đạt hoặc chưa xác định.');
      return;
    }
    if (!verification.length) {
      setErrorMessage('Không tìm thấy lỗi nào từ kết quả chẩn đoán để xác nhận.');
      return;
    }
    setIsLoading(true);
    try {
      await ticketService.saveInspection(ticket.id, 'Functional', functional);
      await ticketService.saveInspection(ticket.id, 'Appearance', appearance);
      await ticketService.saveInspection(ticket.id, 'RepairVerification', verification);
      await Promise.all(Object.entries(appearanceFiles)
        .filter(([, file]) => file)
        .map(([index, file]) => {
          const formData = new FormData();
          formData.append('file', file!);
          formData.append('imageType', 'AfterRepair');
          formData.append('caption', `Inspection ngoại hình: ${appearance[Number(index)].itemName}`);
          return ticketService.uploadImage(ticket.id, formData);
        }));
      await ticketService.passQualityCheck(ticket.id, {
        functionalCheckNotes: summarize(functional),
        cosmeticCheckNotes: summarize(appearance),
        originalIssueResolvedNotes: summarize(verification),
      });
      showSuccess('Đã lưu checklist và QA đạt — thiết bị sẵn sàng bàn giao.');
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  const sections = useMemo(() => [
    { title: '1. Kiểm tra chức năng', type: 'Functional' as const, items: functional, setter: setFunctional, options: functionalStatuses },
    { title: '2. Kiểm tra ngoại hình', type: 'Appearance' as const, items: appearance, setter: setAppearance, options: appearanceStatuses },
    { title: '3. Xác nhận lỗi ban đầu', type: 'RepairVerification' as const, items: verification, setter: setVerification, options: repairStatuses },
  ], [functional, appearance, verification]);

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Checklist kiểm tra chất lượng" footer={
      <>
        <Button variant="secondary" onClick={onClose} disabled={isLoading}>Hủy</Button>
        <Button onClick={handleSubmit} isLoading={isLoading}>Lưu checklist và QA đạt</Button>
      </>
    }>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {sections.map((section) => (
        <section key={section.type} style={{ marginBottom: 20 }}>
          <h4 style={{ margin: '0 0 10px' }}>{section.title}</h4>
          {section.items.map((item, index) => {
            const needsNote = section.type === 'Functional'
              ? ['Fail', 'NotTested'].includes(item.status)
              : section.type === 'Appearance'
                ? ['Damaged', 'Unknown'].includes(item.status)
                : ['PartiallyFixed', 'NotFixed', 'UnableToVerify'].includes(item.status);
            return (
              <div key={`${item.templateId ?? item.itemName}-${index}`} style={{ padding: '10px 0', borderTop: '1px solid var(--color-border)' }}>
                <strong style={{ display: 'block', marginBottom: 6 }}>{item.itemName}</strong>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                  {section.options.map((status) => (
                    <label key={status} style={{ fontSize: 13 }}>
                      <input type="radio" name={`${section.type}-${index}`} checked={item.status === status}
                        onChange={() => update(section.setter, index, { status })} /> {status}
                    </label>
                  ))}
                </div>
                {needsNote && (
                  <>
                    <input value={item.note ?? ''} placeholder="Ghi chú bắt buộc"
                      onChange={(event) => update(section.setter, index, { note: event.target.value })}
                      style={{ width: '100%', boxSizing: 'border-box', marginTop: 8, padding: 8 }} />
                    {section.type === 'Appearance' && item.status === 'Damaged' && (
                      <input type="file" accept="image/*" onChange={(event) =>
                        setAppearanceFiles((files) => ({ ...files, [index]: event.target.files?.[0] }))}
                        style={{ width: '100%', marginTop: 8 }} />
                    )}
                  </>
                )}
              </div>
            );
          })}
        </section>
      ))}
    </Modal>
  );
}

function mergeItems(templates: InspectionTemplate[], saved: InspectionItem[], fallback: string) {
  return templates.map((template) => {
    const existing = saved.find((item) => item.templateId === template.id);
    return existing ?? { templateId: template.id, itemName: template.name, status: fallback, note: '' };
  });
}

function summarize(items: InspectionItem[]) {
  return items.map((item) => `${item.itemName}: ${item.status}${item.note ? ` - ${item.note}` : ''}`).join('; ');
}
