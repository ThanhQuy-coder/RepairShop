import { type ChangeEvent, useEffect, useState } from 'react';
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
  const [existingBeforeCount, setExistingBeforeCount] = useState(0);
  const [existingTotalCount, setExistingTotalCount] = useState(0);
  const [isLoadingImages, setIsLoadingImages] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  useEffect(() => {
    let isMounted = true;
    setIsLoadingImages(true);
    ticketService
      .getImages(ticket.id)
      .then((images) => {
        if (isMounted) {
          setExistingBeforeCount(
            images.filter((image) => image.imageType === 'BeforeRepair').length
          );
          setExistingTotalCount(images.length);
        }
      })
      .catch((err) => {
        if (isMounted) setErrorMessage(extractApiError(err).message);
      })
      .finally(() => {
        if (isMounted) setIsLoadingImages(false);
      });

    return () => {
      isMounted = false;
    };
  }, [ticket.id]);

  const handleFilesSelected = (e: ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;

    const selectedFiles = Array.from(e.target.files);
    const availableSlots = Math.min(6 - existingBeforeCount, 12 - existingTotalCount);
    if (selectedFiles.length > availableSlots) {
      setFiles([]);
      setErrorMessage(
        `Ticket còn tối đa ${availableSlots} ảnh trước sửa chữa (tối đa 6 ảnh trước sửa chữa và 12 ảnh tổng).`
      );
      e.target.value = '';
      return;
    }

    setErrorMessage(null);
    setUploadedCount(0);
    setFiles(selectedFiles);
  };

  const handleUploadAll = async () => {
    if (files.length > Math.min(6 - existingBeforeCount, 12 - existingTotalCount)) {
      setErrorMessage('Số ảnh trước sửa chữa vượt quá giới hạn cho phép.');
      return;
    }

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
        disabled={isLoadingImages || isUploading}
      />

      {files.length > 0 && (
        <p className={styles.hint}>
          Đã chọn {files.length}/6 ảnh trước sửa chữa{' '}
          {isUploading && `— đang tải lên (${uploadedCount}/${files.length})`}
        </p>
      )}

      <div className={styles.actions}>
        <Button variant="secondary" onClick={onDone} disabled={isLoadingImages || isUploading}>
          Bỏ qua bước này
        </Button>
        <Button
          onClick={handleUploadAll}
          isLoading={isUploading}
          disabled={isLoadingImages || files.length === 0}
        >
          Tải ảnh lên & Hoàn tất
        </Button>
      </div>
    </div>
  );
}
