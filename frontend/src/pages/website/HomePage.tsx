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
    let isMounted = true;

    // Dùng Promise.allSettled để tránh việc 1 API lỗi làm hỏng toàn bộ trang
    Promise.allSettled([contentService.getPublicServices(), reviewService.getPublicReviews()])
      .then(([serviceRes, reviewRes]) => {
        if (!isMounted) return;

        if (serviceRes.status === 'fulfilled') {
          setServices(serviceRes.value.items);
        }
        if (reviewRes.status === 'fulfilled') {
          setReviews(reviewRes.value.items);
        }

        if (serviceRes.status === 'rejected' && reviewRes.status === 'rejected') {
          setErrorMessage(extractApiError(serviceRes.reason).message);
        }
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <div className={styles.page}>
      {/* 1. HERO SECTION */}
      <section className={styles.hero}>
        <div className={styles.heroCopy}>
          <span className={styles.eyebrow}>REPAIRSHOP / CARE DESK</span>
          <h1>Sửa chữa minh bạch. Thiết bị trở lại đúng hẹn.</h1>
          <p>
            Từ tiếp nhận đến bàn giao, mọi bước sửa chữa đều được ghi nhận rõ ràng bởi đội ngũ kỹ
            thuật chuyên nghiệp.
          </p>
          <div className={styles.actions}>
            <Link to="/book">
              <Button size="lg">Đặt lịch ngay</Button>
            </Link>
            <Link to="/services" className={styles.secondaryAction}>
              Xem dịch vụ <span>→</span>
            </Link>
          </div>
        </div>

        {/* Khối minh họa / Tra cứu nhanh */}
        <div className={styles.heroPanel}>
          <div className={styles.panelStatus}>
            <span /> Đang tiếp nhận thiết bị
          </div>
          <strong>Theo dõi tiến độ dễ dàng</strong>
          <p>Nhập mã đơn hàng để tra cứu trạng thái sửa chữa theo thời gian thực.</p>
          <div className={styles.progressLabels}>
            <span>Tiếp nhận</span>
            <span>Sửa chữa</span>
            <span>Hoàn tất</span>
          </div>
        </div>
      </section>

      {/* 2. STATS / CON SỐ NỔI BẬT */}
      <section className={styles.statsSection}>
        <div className={styles.statItem}>
          <strong>10,000+</strong>
          <span>Thiết bị sửa thành công</span>
        </div>
        <div className={styles.statItem}>
          <strong>99%</strong>
          <span>Bàn giao đúng hẹn</span>
        </div>
        <div className={styles.statItem}>
          <strong>6 Tháng</strong>
          <span>Bảo hành tối thiểu</span>
        </div>
      </section>

      {/* 3. QUY TRÌNH WORKFLOW */}
      <section className={styles.process}>
        <div className={styles.sectionHeading}>
          <span className={styles.eyebrow}>QUY TRÌNH CHUẨN</span>
          <h2>3 bước đơn giản để khôi phục thiết bị</h2>
        </div>
        <div className={styles.processGrid}>
          {[
            [
              '01',
              'Đặt lịch & Gửi máy',
              'Tiếp nhận nhanh chóng, ghi nhận trạng thái ban đầu rõ ràng.',
            ],
            [
              '02',
              'Chẩn đoán & Báo giá',
              'Kỹ thuật viên kiểm tra minh bạch, chỉ tiến hành khi bạn đồng ý.',
            ],
            [
              '03',
              'Nhận lại thiết bị',
              'Theo dõi tiến độ online, nhận máy kèm tem bảo hành uy tín.',
            ],
          ].map(([number, title, description]) => (
            <div key={number} className={styles.processCard}>
              <strong>{number}</strong>
              <h3>{title}</h3>
              <p>{description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* 4. DỊCH VỤ NỔI BẬT */}
      <section className={styles.section}>
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.eyebrow}>DỊCH VỤ NỔI BẬT</span>
            <h2>Chăm sóc đúng cách cho thiết bị của bạn.</h2>
          </div>
          <Link to="/services">Xem toàn bộ dịch vụ →</Link>
        </div>

        {isLoading && <Loading />}
        {errorMessage && <ErrorMessage message={errorMessage} />}

        {!isLoading && (
          <div className={styles.serviceGrid}>
            {services.slice(0, 3).map((service) => (
              <Link to={`/services/${service.id}`} className={styles.serviceCard} key={service.id}>
                <span className={styles.serviceType}>
                  {service.deviceType ?? 'Thiết bị điện tử'}
                </span>
                <h3>{service.name}</h3>
                <p>{service.description ?? 'Dịch vụ kiểm tra và sửa chữa chuyên nghiệp.'}</p>
                <strong>
                  {service.basePrice !== null
                    ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ`
                    : 'Liên hệ báo giá'}
                </strong>
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* 5. VÌ SAO CHỌN REPAIRSHOP */}
      <section className={styles.trustSection}>
        <div>
          <span className={styles.eyebrow}>VÌ SAO CHỌN REPAIRSHOP</span>
          <h2>Sự yên tâm đến từ sự minh bạch.</h2>
        </div>
        <div className={styles.trustGrid}>
          <div>
            <strong>01</strong>
            <h3>Báo giá trước khi làm</h3>
            <p>Không chi phí ẩn, mọi chi phí linh kiện và công sửa đều công khai.</p>
          </div>
          <div>
            <strong>02</strong>
            <h3>Linh kiện chuẩn</h3>
            <p>Cam kết sử dụng linh kiện chất lượng, có xuất xứ rõ ràng.</p>
          </div>
          <div>
            <strong>03</strong>
            <h3>Lịch sử lưu trữ Online</h3>
            <p>Tra cứu lại lịch sử sửa chữa và thời hạn bảo hành mọi lúc.</p>
          </div>
        </div>
      </section>

      {/* 6. ĐÁNH GIÁ KHÁCH HÀNG */}
      {reviews.length > 0 && (
        <section className={styles.section}>
          <div className={styles.sectionHeading}>
            <div>
              <span className={styles.eyebrow}>ĐÁNH GIÁ THỰC TẾ</span>
              <h2>Khách hàng nói gì về chúng tôi.</h2>
            </div>
          </div>
          <div className={styles.reviewGrid}>
            {reviews.slice(0, 3).map((review) => (
              <blockquote key={review.id}>
                <div className={styles.stars}>
                  {'★'.repeat(review.rating)}
                  <span>{'☆'.repeat(5 - review.rating)}</span>
                </div>
                <p>“{review.comment ?? 'Dịch vụ tốt và chuyên nghiệp.'}”</p>
                <cite>{review.customerName}</cite>
              </blockquote>
            ))}
          </div>
        </section>
      )}

      {/* 7. FINAL CTA (Khóa luồng chuyển đổi) */}
      <section className={styles.bottomCta}>
        <h2>Thiết bị của bạn đang gặp sự cố?</h2>
        <p>Đặt lịch ngay hôm nay để được kiểm tra và tư vấn miễn phí.</p>
        <Link to="/book">
          <Button size="lg">Đặt lịch sửa chữa ngay</Button>
        </Link>
      </section>
    </div>
  );
}
