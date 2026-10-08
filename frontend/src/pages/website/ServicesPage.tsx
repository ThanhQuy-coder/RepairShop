import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { contentService } from '../../services/contentService';
import { extractApiError } from '../../utils/apiError';
import type { ServiceItem } from '../../types/content.types';
import styles from './ServicesPage.module.css';

type SortOrder = 'default' | 'price-asc' | 'price-desc';

export default function ServicesPage() {
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [search, setSearch] = useState('');
  const [deviceType, setDeviceType] = useState('all');
  const [sort, setSort] = useState<SortOrder>('default');
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const pageSize = 9;

  useEffect(() => {
    contentService.getPublicServices()
      .then((result) => setServices(result.items))
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  }, []);

  const deviceTypes = useMemo(
    () => [...new Set(services.map((service) => service.deviceType).filter((type): type is string => Boolean(type)))],
    [services]
  );
  const filteredServices = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    const result = services.filter((service) => {
      const matchesSearch = !normalizedSearch || [service.name, service.description ?? '', service.deviceType ?? ''].some((value) => value.toLowerCase().includes(normalizedSearch));
      return matchesSearch && (deviceType === 'all' || service.deviceType === deviceType);
    });
    return [...result].sort((a, b) => {
      if (sort === 'price-asc') return (a.basePrice ?? Number.MAX_SAFE_INTEGER) - (b.basePrice ?? Number.MAX_SAFE_INTEGER);
      if (sort === 'price-desc') return (b.basePrice ?? -1) - (a.basePrice ?? -1);
      return 0;
    });
  }, [deviceType, search, services, sort]);
  const totalPages = Math.max(1, Math.ceil(filteredServices.length / pageSize));
  const visibleServices = filteredServices.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => setPage(1), [deviceType, search, sort]);
  useEffect(() => { if (page > totalPages) setPage(totalPages); }, [page, totalPages]);

  return <main className={styles.page}>
    <header className={styles.header}><span className={styles.kicker}>DỊCH VỤ & BẢNG GIÁ</span><h1>Chọn đúng dịch vụ cho thiết bị của bạn.</h1><p>Giá tham khảo minh bạch. Tình trạng thực tế sẽ được kỹ thuật viên kiểm tra trước khi sửa chữa.</p></header>
    <section className={styles.toolbar} aria-label="Tìm kiếm và lọc dịch vụ">
      <label className={styles.search}><span>⌕</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm dịch vụ, thiết bị..." aria-label="Tìm dịch vụ" />{search && <button type="button" onClick={() => setSearch('')} aria-label="Xóa tìm kiếm">×</button>}</label>
      <select value={deviceType} onChange={(event) => setDeviceType(event.target.value)} aria-label="Lọc theo thiết bị"><option value="all">Tất cả thiết bị</option>{deviceTypes.map((type) => <option value={type} key={type}>{type}</option>)}</select>
      <select value={sort} onChange={(event) => setSort(event.target.value as SortOrder)} aria-label="Sắp xếp"><option value="default">Sắp xếp mặc định</option><option value="price-asc">Giá thấp đến cao</option><option value="price-desc">Giá cao đến thấp</option></select>
    </section>
    {isLoading && <Loading />}
    {errorMessage && <ErrorMessage message={errorMessage} />}
    {!isLoading && !errorMessage && visibleServices.length === 0 && <EmptyState message="Không tìm thấy dịch vụ phù hợp." />}
    {!isLoading && !errorMessage && visibleServices.length > 0 && <><div className={styles.grid}>{visibleServices.map((service) => <article className={styles.card} key={service.id}>
      <div className={styles.cardTop}><span className={styles.icon}>RS</span><span className={service.isActive ? styles.available : styles.unavailable}>{service.isActive ? 'Đang nhận' : 'Tạm ngưng'}</span></div>
      <span className={styles.type}>{service.deviceType ?? 'Thiết bị điện tử'}</span><h2>{service.name}</h2><p>{service.description ?? 'Kiểm tra và sửa chữa theo quy trình RepairShop.'}</p>
      <div className={styles.cardFooter}><strong>{service.basePrice !== null ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ` : 'Liên hệ báo giá'}</strong><Link to={`/services/${service.id}`}>Xem chi tiết →</Link></div>
      <Link to={`/book?serviceId=${service.id}`}><Button size="sm" className={styles.bookButton} disabled={!service.isActive}>Đặt lịch</Button></Link>
    </article>)}</div><Pagination page={page} pageSize={pageSize} total={filteredServices.length} onPageChange={setPage} /></>}
  </main>;
}
