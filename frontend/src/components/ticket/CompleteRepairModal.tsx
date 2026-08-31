import { type ChangeEvent, useEffect, useState } from 'react';
import { Modal, Button, Select, Input, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { partService } from '../../services/partService';
import { extractApiError } from '../../utils/apiError';
import type { Part } from '../../types/inventory.types';

interface Props {
  isOpen: boolean;
  ticketId: string;
  onClose: () => void;
  onDone: () => void;
}

export default function CompleteRepairModal({ isOpen, ticketId, onClose, onDone }: Props) {
  const [parts, setParts] = useState<Part[]>([]);
  const [selectedPartId, setSelectedPartId] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [usedParts, setUsedParts] = useState<{ name: string; quantity: number }[]>([]);

  const [repairNote, setRepairNote] = useState('');
  const [completionNotes, setCompletionNotes] = useState('');
  const [afterFile, setAfterFile] = useState<File | null>(null);

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) partService.list().then(setParts);
  }, [isOpen]);

  const handleAddPart = async () => {
    if (!selectedPartId) return;
    setErrorMessage(null);
    try {
      const result = await ticketService.usePart(ticketId, selectedPartId, quantity);
      setUsedParts((prev) => [...prev, { name: result.partName, quantity: result.quantity }]);
      setSelectedPartId('');
      setQuantity(1);
    } catch (err) {
      setErrorMessage(extractApiError(err).message); // VD: 409 INSUFFICIENT_STOCK — Backend enforce BR-20
    }
  };

  const handleFinish = async () => {
    if (!completionNotes.trim()) {
      setErrorMessage('Vui lòng nhập ghi chú hoàn tất trước khi kết thúc.');
      return;
    }
    setIsSaving(true);
    setErrorMessage(null);
    try {
      if (repairNote.trim()) await ticketService.addRepairNote(ticketId, repairNote);

      if (afterFile) {
        const formData = new FormData();
        formData.append('file', afterFile);
        formData.append('imageType', 'AfterRepair');
        await ticketService.uploadImage(ticketId, formData);
      }

      await ticketService.recordCompletionNotes(ticketId, completionNotes);
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Hoàn tất sửa chữa"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Đóng
          </Button>
          <Button onClick={handleFinish} isLoading={isSaving}>
            Xác nhận hoàn tất
          </Button>
        </>
      }
    >
      {errorMessage && <ErrorMessage message={errorMessage} />}

      <h4 style={{ marginBottom: 8 }}>Linh kiện đã sử dụng</h4>
      <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
        <Select
          placeholder="-- Chọn linh kiện --"
          value={selectedPartId}
          options={parts.map((p) => ({
            value: p.id,
            label: `${p.name} (${p.unitPrice.toLocaleString('vi-VN')}đ)`,
          }))}
          onChange={(e) => setSelectedPartId(e.target.value)}
        />
        <Input
          type="number"
          min={1}
          style={{ width: 70 }}
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
        />
        <Button type="button" size="sm" onClick={handleAddPart}>
          Thêm
        </Button>
      </div>
      {usedParts.length > 0 && (
        <ul style={{ fontSize: 13, marginBottom: 16, paddingLeft: 20 }}>
          {usedParts.map((p, i) => (
            <li key={i}>
              {p.name} × {p.quantity}
            </li>
          ))}
        </ul>
      )}

      <h4 style={{ marginBottom: 8 }}>Ghi chú sửa chữa</h4>
      <textarea
        rows={2}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
          marginBottom: 16,
        }}
        value={repairNote}
        onChange={(e) => setRepairNote(e.target.value)}
        placeholder="Ghi log thao tác trong quá trình sửa..."
      />

      <h4 style={{ marginBottom: 8 }}>Ảnh sau sửa</h4>
      <input
        type="file"
        accept="image/*"
        onChange={(e: ChangeEvent<HTMLInputElement>) => setAfterFile(e.target.files?.[0] ?? null)}
        style={{ marginBottom: 16 }}
      />

      <h4 style={{ marginBottom: 8 }}>Ghi chú hoàn tất *</h4>
      <textarea
        rows={2}
        style={{
          width: '100%',
          padding: 10,
          border: '1px solid var(--color-border)',
          borderRadius: 4,
        }}
        value={completionNotes}
        onChange={(e) => setCompletionNotes(e.target.value)}
        placeholder="Bắt buộc trước khi chuyển sang kiểm thử QA (Task 4.10)..."
      />
    </Modal>
  );
}
