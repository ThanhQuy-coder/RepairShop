import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { contentService } from '../../services/contentService';
import { extractApiError } from '../../utils/apiError';
import type { ServiceItem } from '../../types/content.types';
import styles from './ServiceDetailPage.module.css';

export default function ServiceDetailPage() {
  const { id } = useParams();
  const [service, setService] = useState<ServiceItem | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    contentService.getPublicServices().then((result) => {
      setService(result.items.find((item) => item.id === id) ?? null);
    }).catch((error) => setErrorMessage(extractApiError(error).message));
  }, [id]);

  if (errorMessage) return <div className={styles.wrapper}><ErrorMessage message={errorMessage} /></div>;
  if (!service) return <div className={styles.wrapper}><Loading /></div>;

  return <main className={styles.wrapper}>
    <Link to="/services" className={styles.back}>← Tất cả dịch vụ</Link>
    <section className={styles.card}>
      <span className={styles.kicker}>{service.deviceType ?? 'THIẾT BỊ ĐIỆN TỬ'}</span>
      <h1>{service.name}</h1>
      <p className={styles.description}>{service.description ?? 'Dịch vụ kiểm tra và sửa chữa theo quy trình chuyên nghiệp của RepairShop.'}</p>
      <div className={styles.meta}><div><span>Giá tham khảo</span><strong>{service.basePrice !== null ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ` : 'Liên hệ báo giá'}</strong></div><div><span>Trạng thái</span><strong>{service.isActive ? 'Đang nhận' : 'Tạm ngưng'}</strong></div></div>
      <div className={styles.actions}><Link to={`/book?serviceId=${service.id}`}><Button size="lg" disabled={!service.isActive}>Đặt lịch dịch vụ này</Button></Link><Link to="/track" className={styles.secondary}>Tra cứu phiếu sửa chữa</Link></div>
    </section>
  </main>;
}
