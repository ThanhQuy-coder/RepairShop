import type { TicketImage } from '../../types/ticket.types';
import { EmptyState } from '../common';
import styles from './TicketImageGallery.module.css';

interface TicketImageGalleryProps {
  images: TicketImage[];
}

const TYPE_LABEL: Record<string, string> = {
  BeforeRepair: 'Trước sửa',
  AfterRepair: 'Sau sửa',
  Other: 'Khác',
};

export default function TicketImageGallery({ images }: TicketImageGalleryProps) {
  if (images.length === 0) return <EmptyState message="Chưa có ảnh nào." />;

  const groups: Record<string, TicketImage[]> = { BeforeRepair: [], AfterRepair: [], Other: [] };
  images.forEach((img) => groups[img.imageType]?.push(img));

  return (
    <div>
      {Object.entries(groups).map(([type, imgs]) =>
        imgs.length === 0 ? null : (
          <div key={type} className={styles.group}>
            <h4 className={styles.groupTitle}>
              {TYPE_LABEL[type]} ({imgs.length})
            </h4>
            <div className={styles.grid}>
              {imgs.map((img) => (
                <a
                  key={img.id}
                  href={img.imageUrl}
                  target="_blank"
                  rel="noreferrer"
                  className={styles.thumb}
                >
                  <img src={img.imageUrl} alt={TYPE_LABEL[type]} />
                </a>
              ))}
            </div>
          </div>
        )
      )}
    </div>
  );
}
