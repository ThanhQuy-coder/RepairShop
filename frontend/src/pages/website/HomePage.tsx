import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { contentService } from '../../services/contentService';
import { reviewService } from '../../services/reviewService';
import { extractApiError } from '../../utils/apiError';
import type { ServiceItem } from '../../types/content.types';
import type { ReviewListItem } from '../../types/review.types';
import styles from './HomePage.module.css';

export default function HomePage() {
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([contentService.getPublicServices(), reviewService.getPublicReviews()])
      .then(([serviceResult, reviewResult]) => {
        setServices(serviceResult.items);
        setReviews(reviewResult.items);
      })
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <div className={styles.heroCopy}>
          <span className={styles.eyebrow}>REPAIRSHOP / CARE DESK</span>
          <h1>Sửa chữa minh bạch. Thiết bị trở lại đúng hẹn.</h1>
          <p>
            Từ tiếp nhận đến bàn giao, mọi bước sửa chữa đều được ghi nhận rõ ràng bởi đội ngũ kỹ
            thuật chuyên nghiệp.
          </p>
          <div className={styles.actions}>
            <Link to="/services"><Button size="lg">Xem dịch vụ</Button></Link>
            <Link to="/book" className={styles.secondaryAction}>Đặt lịch ngay <span>→</span></Link>
          </div>
        </div>
        <div className={styles.heroPanel}>
          <div className={styles.panelStatus}><span /> Đang tiếp nhận thiết bị</div>
          <strong>01 — 04</strong>
          <p>Đặt lịch thuận tiện, theo dõi tiến độ, nhận thông báo đúng lúc.</p>
          <div className={styles.progress}><span /></div>
          <div className={styles.progressLabels}><span>Tiếp nhận</span><span>Hoàn tất</span></div>
        </div>
      </section>

      <section className={styles.process}>
        {[
          ['01', 'Gửi thiết bị', 'Tiếp nhận và ghi nhận tình trạng ban đầu'],
          ['02', 'Kiểm tra', 'Kỹ thuật viên chẩn đoán minh bạch'],
          ['03', 'Nhận cập nhật', 'Theo dõi tiến độ ngay trên hệ thống'],
        ].map(([number, title, description]) => (
          <div key={number}><strong>{number}</strong><h3>{title}</h3><p>{description}</p></div>
        ))}
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeading}>
          <div><span className={styles.eyebrow}>DỊCH VỤ NỔI BẬT</span><h2>Chăm sóc đúng cách cho thiết bị của bạn.</h2></div>
          <Link to="/services">Xem toàn bộ dịch vụ →</Link>
        </div>
        {isLoading && <Loading />}
        {errorMessage && <ErrorMessage message={errorMessage} />}
        {!isLoading && !errorMessage && (
          <div className={styles.serviceGrid}>
            {services.slice(0, 3).map((service) => (
              <Link to={`/services/${service.id}`} className={styles.serviceCard} key={service.id}>
                <span className={styles.serviceIcon}>RS</span>
                <span className={styles.serviceType}>{service.deviceType ?? 'Thiết bị điện tử'}</span>
                <h3>{service.name}</h3>
                <p>{service.description ?? 'Dịch vụ kiểm tra và sửa chữa chuyên nghiệp.'}</p>
                <strong>{service.basePrice !== null ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ` : 'Liên hệ báo giá'}</strong>
              </Link>
            ))}
          </div>
        )}
      </section>

      <section className={styles.trustSection}>
        <div><span className={styles.eyebrow}>VÌ SAO CHỌN REPAIRSHOP</span><h2>Một quy trình đáng tin cậy cho mỗi thiết bị.</h2></div>
        <div className={styles.trustGrid}>
          <div><strong>01</strong><h3>Minh bạch</h3><p>Thông tin tiếp nhận và tiến độ được cập nhật theo từng mốc.</p></div>
          <div><strong>02</strong><h3>Đúng chuyên môn</h3><p>Đội ngũ kỹ thuật làm việc theo quy trình kiểm tra rõ ràng.</p></div>
          <div><strong>03</strong><h3>An tâm sau sửa chữa</h3><p>Lịch sử sửa chữa và bảo hành được lưu trữ tập trung.</p></div>
        </div>
      </section>

      {reviews.length > 0 && (
        <section className={styles.section}>
          <div className={styles.sectionHeading}><div><span className={styles.eyebrow}>KHÁCH HÀNG NÓI GÌ</span><h2>Những trải nghiệm thật từ khách hàng.</h2></div></div>
          <div className={styles.reviewGrid}>{reviews.slice(0, 3).map((review) => (
            <blockquote key={review.id}><div className={styles.stars}>{'★'.repeat(review.rating)}<span>{'★'.repeat(5 - review.rating)}</span></div><p>“{review.comment ?? 'Dịch vụ tốt và chuyên nghiệp.'}”</p><cite>{review.customerName}</cite></blockquote>
          ))}</div>
        </section>
      )}
    </div>
  );
}
