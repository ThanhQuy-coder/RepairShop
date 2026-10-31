import type { InspectionResponse, InspectionType } from '../../types/inspection.types';
import styles from './TicketInspectionSummary.module.css';

interface Props {
  results: Partial<Record<InspectionType, InspectionResponse>>;
  isLoading?: boolean;
}

const sections: Array<{ type: InspectionType; title: string; icon: string }> = [
  { type: 'Functional', title: 'Kiểm tra chức năng', icon: '⚙️' },
  { type: 'Appearance', title: 'Kiểm tra ngoại hình', icon: '🔎' },
  { type: 'RepairVerification', title: 'Xác nhận khắc phục lỗi', icon: '✓' },
];

const statusLabels: Record<string, string> = {
  Pass: 'Đạt',
  Fail: 'Không đạt',
  NotApplicable: 'Không áp dụng',
  NotTested: 'Chưa kiểm tra',
  Normal: 'Bình thường',
  Damaged: 'Hư hỏng',
  Unknown: 'Không xác định',
  Fixed: 'Đã khắc phục',
  PartiallyFixed: 'Khắc phục một phần',
  NotFixed: 'Chưa khắc phục',
  UnableToVerify: 'Không thể xác nhận',
};

export default function TicketInspectionSummary({ results, isLoading }: Props) {
  return (
    <section className={styles.section}>
      <div className={styles.heading}>
        <div>
          <h3>Kiểm tra chất lượng (QA)</h3>
          <p>Ba nhóm kiểm tra được lưu riêng theo từng ticket.</p>
        </div>
      </div>
      {isLoading ? (
        <p className={styles.empty}>Đang tải kết quả kiểm tra...</p>
      ) : (
        <div className={styles.grid}>
          {sections.map((section) => {
            const result = results[section.type];
            return (
              <article className={styles.card} key={section.type}>
                <h4><span aria-hidden="true">{section.icon}</span>{section.title}</h4>
                {!result?.items.length ? (
                  <p className={styles.empty}>Chưa có kết quả.</p>
                ) : (
                  <div className={styles.items}>
                    {result.items.map((item) => (
                      <div className={styles.item} key={item.id ?? `${section.type}-${item.itemName}`}>
                        <div className={styles.itemHeader}>
                          <span className={styles.itemName}>{item.itemName}</span>
                          <span className={`${styles.status} ${styles[`status${item.status}`] ?? ''}`}>
                            {statusLabels[item.status] ?? item.status}
                          </span>
                        </div>
                        {item.note && <p className={styles.note}>{item.note}</p>}
                      </div>
                    ))}
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}
