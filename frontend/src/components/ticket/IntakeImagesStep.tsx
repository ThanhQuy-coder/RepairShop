import { type ChangeEvent, useState } from 'react';
import { Button, ErrorMessage } from '../common';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { Ticket } from '../../types/ticket.types';
import styles from './IntakeStep.module.css';

interface IntakeImagesStepProps {
  ticket: Ticket;
  onDone: () => void;
}

export default function IntakeImagesStep({ ticket, onDone }: IntakeImagesStepProps) {
  const [files, setFiles] = useState<File[]>([]);
  const [uploadedCount, setUploadedCount] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const handleFilesSelected = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) setFiles(Array.from(e.target.files));
  };

  const handleUploadAll = async () => {
    setErrorMessage(null);
    setIsUploading(true);
    try {
      for (const file of files) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('imageType', 'BeforeRepair'); // khớp enum ImageType (Task 4.5, Tuần 4)
        await ticketService.uploadImage(ticket.id, formData);
        setUploadedCount((prev) => prev + 1);
      }
      onDone();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div>
      <div className={styles.selectedCard}>
        <span>
          ✓ Đã tạo phiếu <strong>{ticket.ticketCode}</strong> — giờ tải ảnh hiện trạng thiết bị
          (không bắt buộc):
        </span>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <input
        type="file"
        accept="image/*"
        multiple
        onChange={handleFilesSelected}
        className={styles.fileInput}
      />

      {files.length > 0 && (
        <p className={styles.hint}>
          Đã chọn {files.length} ảnh{' '}
          {isUploading && `— đang tải lên (${uploadedCount}/${files.length})`}
        </p>
      )}

      <div className={styles.actions}>
        <Button variant="secondary" onClick={onDone} disabled={isUploading}>
          Bỏ qua bước này
        </Button>
        <Button onClick={handleUploadAll} isLoading={isUploading} disabled={files.length === 0}>
          Tải ảnh lên & Hoàn tất
        </Button>
      </div>
    </div>
  );
}
