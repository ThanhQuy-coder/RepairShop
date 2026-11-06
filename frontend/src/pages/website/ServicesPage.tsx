import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Star, Search, Wrench, ArrowRight } from '../../components/common/FixoraIcons';
import { Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { contentService } from '../../services/contentService';
import { extractApiError } from '../../utils/apiError';
import type { ServiceItem } from '../../types/content.types';
import { DEVICE_TYPE_OPTIONS, DEVICE_TYPE_LABELS } from '../../types/device.types';
import { resolveServiceImageUrl, handleServiceImageError } from '../../utils/imageUrl';
import styles from './ServicesPage.module.css';

type SortOrder = 'default' | 'rating-desc' | 'price-asc' | 'price-desc';
const DEVICE_TYPES = DEVICE_TYPE_OPTIONS.map((o) => o.value);

export default function ServicesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialDeviceType = searchParams.get('deviceType') || 'all';
  const initialSearch = searchParams.get('search') || '';

  const [services, setServices] = useState<ServiceItem[]>([]);
  const [search, setSearch] = useState(initialSearch);
  const [deviceType, setDeviceType] = useState(initialDeviceType);
  const [sort, setSort] = useState<SortOrder>('default');
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const pageSize = 9;

  useEffect(() => {
    const paramDevice = searchParams.get('deviceType') || 'all';
    const paramSearch = searchParams.get('search') || '';
    setDeviceType(paramDevice);
    setSearch(paramSearch);
  }, [searchParams]);

  useEffect(() => {
    setIsLoading(true);
    contentService
      .getPublicServices()
      .then((result) => setServices(result.items))
      .catch((error) => setErrorMessage(extractApiError(error).message))
      .finally(() => setIsLoading(false));
  }, []);

  const handleSelectDeviceType = (nextType: string) => {
    setDeviceType(nextType);
    const nextParams = new URLSearchParams(searchParams);
    if (nextType === 'all') {
      nextParams.delete('deviceType');
    } else {
      nextParams.set('deviceType', nextType);
    }
    setSearchParams(nextParams, { replace: true });
  };

  const filteredServices = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    const result = services.filter((service) => {
      const matchesSearch =
        !normalizedSearch ||
        [
          service.name,
          service.description ?? '',
          service.deviceType ?? '',
          service.deviceType ? DEVICE_TYPE_LABELS[service.deviceType] ?? '' : '',
        ].some((value) => value.toLowerCase().includes(normalizedSearch));
      const matchesDevice =
        deviceType === 'all' ||
        (service.deviceType ?? '').toLowerCase() === deviceType.toLowerCase();
      return matchesSearch && matchesDevice;
    });

    return [...result].sort((a, b) => {
      if (sort === 'rating-desc') {
        if ((b.averageRating ?? 0) !== (a.averageRating ?? 0)) {
          return (b.averageRating ?? 0) - (a.averageRating ?? 0);
        }
        return (b.reviewCount ?? 0) - (a.reviewCount ?? 0);
      }
      if (sort === 'price-asc') {
        return (a.basePrice ?? Number.MAX_SAFE_INTEGER) - (b.basePrice ?? Number.MAX_SAFE_INTEGER);
      }
      if (sort === 'price-desc') {
        return (b.basePrice ?? -1) - (a.basePrice ?? -1);
      }
      return 0;
    });
  }, [deviceType, search, services, sort]);

  const totalPages = Math.max(1, Math.ceil(filteredServices.length / pageSize));
  const visibleServices = filteredServices.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => setPage(1), [deviceType, search, sort]);
  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <span className={styles.kicker}>FIXORA SERVICE CATALOG · DỊCH VỤ & BẢNG GIÁ</span>
        <h1>Chọn đúng dịch vụ chăm sóc cho thiết bị của bạn.</h1>
        <p>
          Bảng giá tham khảo minh bạch theo từng dòng máy. Kỹ thuật viên Fixora kiểm tra lỗi miễn phí và báo giá chính xác trước khi tiến hành sửa chữa.
        </p>
      </header>

      <div className={styles.categoryPills} role="tablist" aria-label="Phân loại thiết bị">
        <button
          type="button"
          className={`${styles.pill} ${deviceType === 'all' ? styles.pillActive : ''}`}
          onClick={() => handleSelectDeviceType('all')}
        >
          Tất cả ({services.length})
        </button>
        {DEVICE_TYPES.map((type) => {
          const count = services.filter(
            (s) => (s.deviceType ?? '').toLowerCase() === type.toLowerCase()
          ).length;
          return (
            <button
              key={type}
              type="button"
              className={`${styles.pill} ${
                deviceType.toLowerCase() === type.toLowerCase() ? styles.pillActive : ''
              }`}
              onClick={() => handleSelectDeviceType(type)}
            >
              {DEVICE_TYPE_LABELS[type] || type} ({count})
            </button>
          );
        })}
      </div>

      <section className={styles.toolbar} aria-label="Tìm kiếm và lọc dịch vụ">
        <label className={styles.search}>
          <Search size={18} className={styles.searchIcon} />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm dịch vụ (VD: thay màn hình, thay pin, bản lề, chống nước...)"
            aria-label="Tìm dịch vụ"
          />
          {search && (
            <button
              type="button"
              onClick={() => setSearch('')}
              aria-label="Xóa tìm kiếm"
            >
              ×
            </button>
          )}
        </label>
        <select
          value={deviceType}
          onChange={(event) => handleSelectDeviceType(event.target.value)}
          aria-label="Lọc theo thiết bị"
        >
          <option value="all">Tất cả loại thiết bị</option>
          {DEVICE_TYPES.map((type) => (
            <option value={type} key={type}>
              {DEVICE_TYPE_LABELS[type] || type}
            </option>
          ))}
        </select>
        <select
          value={sort}
          onChange={(event) => setSort(event.target.value as SortOrder)}
          aria-label="Sắp xếp"
        >
          <option value="default">Sắp xếp mặc định</option>
          <option value="rating-desc">Đánh giá cao nhất</option>
          <option value="price-asc">Giá thấp đến cao</option>
          <option value="price-desc">Giá cao đến thấp</option>
        </select>
      </section>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}
      {!isLoading && !errorMessage && visibleServices.length === 0 && (
        <EmptyState message="Không tìm thấy dịch vụ phù hợp với bộ lọc hiện tại." />
      )}

      {!isLoading && !errorMessage && visibleServices.length > 0 && (
        <>
          <div className={styles.grid}>
            {visibleServices.map((service) => (
              <article className={styles.card} key={service.id}>
                <Link to={`/services/${service.id}`} className={styles.imageLink}>
                  <div className={styles.imageWrap}>
                    <img
                      src={resolveServiceImageUrl(service.imageUrl, service.deviceType)}
                      alt={service.name}
                      className={styles.serviceImage}
                      loading="lazy"
                      onError={handleServiceImageError}
                    />
                    <span className={styles.deviceBadge}>
                      <Wrench size={12} />
                      {service.deviceType
                        ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
                        : 'Thiết bị điện tử'}
                    </span>
                    <span
                      className={
                        service.isActive ? styles.availableBadge : styles.unavailableBadge
                      }
                    >
                      {service.isActive ? 'Đang nhận' : 'Tạm ngưng'}
                    </span>
                  </div>
                </Link>

                <div className={styles.cardBody}>
                  <div className={styles.ratingRow}>
                    {service.reviewCount && service.reviewCount > 0 ? (
                      <div className={styles.ratingBadge}>
                        <Star size={14} className={styles.starIcon} />
                        <strong>{(service.averageRating ?? 0).toFixed(1)}</strong>
                        <span>({service.reviewCount} đánh giá)</span>
                      </div>
                    ) : (
                      <span className={styles.verifiedTag}>Bảo hành tiêu chuẩn Fixora</span>
                    )}
                  </div>

                  <h2>
                    <Link to={`/services/${service.id}`} className={styles.titleLink}>
                      {service.name}
                    </Link>
                  </h2>

                  <p className={styles.desc}>
                    {service.description ??
                      'Kiểm tra và sửa chữa theo quy trình kỹ thuật tiêu chuẩn của Fixora.'}
                  </p>

                  <div className={styles.cardFooter}>
                    <div className={styles.priceBlock}>
                      <span className={styles.priceLabel}>Giá tham khảo</span>
                      <strong>
                        {service.basePrice !== null
                          ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ`
                          : 'Liên hệ báo giá'}
                      </strong>
                    </div>
                    <Link to={`/services/${service.id}`} className={styles.detailLink}>
                      Chi tiết <ArrowRight size={14} />
                    </Link>
                  </div>

                  <Link
                    to={`/book?serviceId=${service.id}${
                      service.deviceType ? `&deviceType=${service.deviceType}` : ''
                    }`}
                    className={styles.bookLink}
                  >
                    <Button
                      size="sm"
                      className={styles.bookButton}
                      disabled={!service.isActive}
                    >
                      Đặt lịch sửa chữa
                    </Button>
                  </Link>
                </div>
              </article>
            ))}
          </div>

          <Pagination
            page={page}
            pageSize={pageSize}
            total={filteredServices.length}
            onPageChange={setPage}
          />
        </>
      )}
    </main>
  );
}
