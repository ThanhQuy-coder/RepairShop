import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Star,
  ShieldCheck,
  Clock,
  Wrench,
  CheckCircle2,
  ArrowLeft,
  MessageSquare,
} from '../../components/common/FixoraIcons';
import { Button, ErrorMessage, Loading, Pagination } from '../../components/common';
import { contentService } from '../../services/contentService';
import { reviewService } from '../../services/reviewService';
import { extractApiError } from '../../utils/apiError';
import type { ServiceItem } from '../../types/content.types';
import type { ServiceReviewListResponse } from '../../types/review.types';
import { DEVICE_TYPE_LABELS } from '../../types/device.types';
import { resolveServiceImageUrl, handleServiceImageError } from '../../utils/imageUrl';
import styles from './ServiceDetailPage.module.css';

const REVIEW_PAGE_SIZE = 5;

export default function ServiceDetailPage() {
  const { id } = useParams();
  const [service, setService] = useState<ServiceItem | null>(null);
  const [reviewData, setReviewData] = useState<ServiceReviewListResponse | null>(null);
  const [reviewPage, setReviewPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isReviewLoading, setIsReviewLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setIsLoading(true);
    setErrorMessage(null);

    contentService
      .getServiceById(id)
      .then((item) => {
        if (item) {
          setService(item);
          return;
        }
        return contentService.getPublicServices().then((result) => {
          const found = result.items.find((s) => s.id === id) ?? null;
          if (!found) {
            setErrorMessage('Không tìm thấy dịch vụ sửa chữa yêu cầu.');
          }
          setService(found);
        });
      })
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  }, [id]);

  useEffect(() => {
    if (!id) return;
    setIsReviewLoading(true);
    reviewService
      .getServiceReviews(id, reviewPage, REVIEW_PAGE_SIZE)
      .then((res) => setReviewData(res))
      .catch(() => {
        // Keep empty review summary on error so service detail still renders cleanly
      })
      .finally(() => setIsReviewLoading(false));
  }, [id, reviewPage]);

  if (isLoading) {
    return (
      <div className={styles.wrapper}>
        <Loading />
      </div>
    );
  }

  if (errorMessage || !service) {
    return (
      <div className={styles.wrapper}>
        <ErrorMessage message={errorMessage ?? 'Không tìm thấy dịch vụ sửa chữa.'} />
        <div style={{ marginTop: 16 }}>
          <Link to="/services" className={styles.back}>
            <ArrowLeft size={16} /> Quay lại danh sách dịch vụ
          </Link>
        </div>
      </div>
    );
  }

  const summary = reviewData?.summary ?? {
    serviceId: service.id,
    averageRating: service.averageRating ?? 0,
    totalReviews: service.reviewCount ?? 0,
    ratingDistribution: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 },
  };
  const reviews = reviewData?.items ?? [];

  return (
    <main className={styles.wrapper}>
      <Link to="/services" className={styles.back}>
        <ArrowLeft size={16} /> Tất cả dịch vụ Fixora
      </Link>

      <section className={styles.heroCard}>
        <div className={styles.imageColumn}>
          <div className={styles.imageFrame}>
            <img
              src={resolveServiceImageUrl(service.imageUrl, service.deviceType)}
              alt={service.name}
              className={styles.heroImage}
              onError={handleServiceImageError}
            />
            <span className={styles.deviceTag}>
              <Wrench size={13} />
              {service.deviceType
                ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
                : 'Thiết bị điện tử'}
            </span>
          </div>

          <div className={styles.guarantees}>
            <div className={styles.guaranteeItem}>
              <ShieldCheck size={18} className={styles.guaranteeIcon} />
              <div>
                <strong>Bảo hành điện tử minh bạch</strong>
                <span>Tra cứu bảo hành trực tuyến theo số Serial/IMEI</span>
              </div>
            </div>
            <div className={styles.guaranteeItem}>
              <Clock size={18} className={styles.guaranteeIcon} />
              <div>
                <strong>Kiểm tra lỗi & Báo giá trước khi sửa</strong>
                <span>Khách hàng duyệt báo giá mới tiến hành thay thế</span>
              </div>
            </div>
          </div>
        </div>

        <div className={styles.infoColumn}>
          <div className={styles.kickerRow}>
            <span className={styles.kicker}>
              FIXORA CARE ·{' '}
              {(service.deviceType
                ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
                : 'THIẾT BỊ ĐIỆN TỬ'
              ).toUpperCase()}
            </span>
            <span className={service.isActive ? styles.activePill : styles.inactivePill}>
              {service.isActive ? 'Đang nhận lịch' : 'Tạm ngưng'}
            </span>
          </div>

          <h1>{service.name}</h1>

          <div className={styles.ratingSummaryLine}>
            <div className={styles.starsInline}>
              {[1, 2, 3, 4, 5].map((star) => (
                <Star
                  key={star}
                  size={16}
                  className={
                    star <= Math.round(summary.averageRating || 5)
                      ? styles.starFilled
                      : styles.starEmpty
                  }
                />
              ))}
            </div>
            {summary.totalReviews > 0 ? (
              <a href="#service-reviews" className={styles.ratingAnchor}>
                <strong>{summary.averageRating.toFixed(1)}/5</strong> ({summary.totalReviews} đánh
                giá từ khách đã sửa chữa)
              </a>
            ) : (
              <span className={styles.noReviewsYet}>
                Cam kết linh kiện tiêu chuẩn · Quy trình kỹ thuật Fixora
              </span>
            )}
          </div>

          <p className={styles.description}>
            {service.description ??
              'Dịch vụ kiểm tra, chẩn đoán và sửa chữa phần cứng theo quy trình kỹ thuật tiêu chuẩn của Fixora.'}
          </p>

          <div className={styles.priceBox}>
            <div>
              <span className={styles.priceLabel}>Chi phí tham khảo</span>
              <strong className={styles.priceValue}>
                {service.basePrice !== null
                  ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ`
                  : 'Liên hệ báo giá'}
              </strong>
              <span className={styles.priceNote}>
                Đã bao gồm công kiểm tra máy · Báo giá chi tiết theo dòng máy cụ thể
              </span>
            </div>
          </div>

          <ul className={styles.checkList}>
            <li>
              <CheckCircle2 size={16} /> Linh kiện tương thích chất lượng cao, nguồn gốc rõ ràng
            </li>
            <li>
              <CheckCircle2 size={16} /> Ký tên lên linh kiện với các ca sửa chữa chuyên sâu
            </li>
            <li>
              <CheckCircle2 size={16} /> Theo dõi tiến độ sửa chữa thời gian thực trên website
            </li>
          </ul>

          <div className={styles.actions}>
            <Link
              to={`/book?serviceId=${service.id}${
                service.deviceType ? `&deviceType=${service.deviceType}` : ''
              }`}
              className={styles.primaryBookLink}
            >
              <Button size="lg" disabled={!service.isActive}>
                Đặt lịch dịch vụ này
              </Button>
            </Link>
            <Link to="/track" className={styles.secondary}>
              Tra cứu phiếu sửa chữa
            </Link>
          </div>
        </div>
      </section>

      <section id="service-reviews" className={styles.reviewsSection}>
        <div className={styles.reviewsHeader}>
          <div>
            <span className={styles.kicker}>ĐÁNH GIÁ THỰC TẾ</span>
            <h2>Khách hàng nói gì sau khi sử dụng dịch vụ</h2>
            <p>
              Tất cả đánh giá đều được ghi nhận từ các phiếu sửa chữa đã hoàn tất và bàn giao thiết bị tại Fixora.
            </p>
          </div>
        </div>

        <div className={styles.reviewOverviewGrid}>
          <div className={styles.scoreBox}>
            <div className={styles.bigScore}>
              {summary.totalReviews > 0 ? summary.averageRating.toFixed(1) : '—'}
            </div>
            <div className={styles.starsInline}>
              {[1, 2, 3, 4, 5].map((star) => (
                <Star
                  key={star}
                  size={18}
                  className={
                    star <= Math.round(summary.averageRating || 0)
                      ? styles.starFilled
                      : styles.starEmpty
                  }
                />
              ))}
            </div>
            <span className={styles.totalReviewsText}>
              {summary.totalReviews > 0
                ? `Dựa trên ${summary.totalReviews} lượt đánh giá xác thực`
                : 'Chưa có lượt đánh giá công khai'}
            </span>
          </div>

          <div className={styles.distributionBox}>
            {[5, 4, 3, 2, 1].map((star) => {
              const count =
                summary.ratingDistribution[star] ??
                (summary.ratingDistribution as Record<string, number>)[String(star)] ??
                0;
              const pct =
                summary.totalReviews > 0
                  ? Math.round((count / summary.totalReviews) * 100)
                  : 0;
              return (
                <div key={star} className={styles.distRow}>
                  <span className={styles.distStarLabel}>
                    {star} <Star size={12} className={styles.starFilled} />
                  </span>
                  <div className={styles.distBarTrack}>
                    <div
                      className={styles.distBarFill}
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                  <span className={styles.distCount}>{count}</span>
                </div>
              );
            })}
          </div>
        </div>

        {isReviewLoading ? (
          <Loading />
        ) : reviews.length === 0 ? (
          <div className={styles.emptyReviews}>
            <MessageSquare size={32} className={styles.emptyIcon} />
            <h3>Chưa có nhận xét nào cho dịch vụ này</h3>
            <p>
              Đánh giá từ khách hàng sẽ tự động hiển thị tại đây sau khi phiếu sửa chữa có sử dụng dịch vụ này được bàn giao.
            </p>
          </div>
        ) : (
          <>
            <div className={styles.reviewList}>
              {reviews.map((rev) => (
                <article key={rev.id} className={styles.reviewCard}>
                  <div className={styles.reviewTop}>
                    <div className={styles.reviewerInfo}>
                      <div className={styles.avatar}>
                        {rev.customerDisplayName.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <div className={styles.reviewerNameRow}>
                          <strong>{rev.customerDisplayName}</strong>
                          <span className={styles.verifiedRepairBadge}>
                            <CheckCircle2 size={12} /> Đã sửa tại Fixora
                          </span>
                        </div>
                        <time className={styles.reviewDate}>
                          {new Date(rev.createdAt).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                          })}
                        </time>
                      </div>
                    </div>

                    <div className={styles.starsInline}>
                      {[1, 2, 3, 4, 5].map((star) => (
                        <Star
                          key={star}
                          size={15}
                          className={
                            star <= rev.rating ? styles.starFilled : styles.starEmpty
                          }
                        />
                      ))}
                    </div>
                  </div>

                  {rev.comment && <p className={styles.reviewComment}>{rev.comment}</p>}
                </article>
              ))}
            </div>

            {summary.totalReviews > REVIEW_PAGE_SIZE && (
              <div className={styles.reviewPagination}>
                <Pagination
                  page={reviewPage}
                  pageSize={REVIEW_PAGE_SIZE}
                  total={summary.totalReviews}
                  onPageChange={setReviewPage}
                />
              </div>
            )}
          </>
        )}
      </section>
    </main>
  );
}
