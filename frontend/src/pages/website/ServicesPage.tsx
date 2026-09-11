import { useEffect, useState } from 'react';

import { contentService } from '../../services/contentService';
import type { ServiceItem } from '../../types/content.types';

import { EmptyState, Loading, Pagination } from '../../components/common';

export default function ServicesPage() {
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);

  const pageSize = 10;

  useEffect(() => {
    contentService
      .getPublicServices()
      .then((res) => setServices(res.items))
      .finally(() => setIsLoading(false));
  }, []);

  const total = services.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  if (isLoading) return <Loading />;

  if (services.length === 0) {
    return <EmptyState message="Chưa có dịch vụ nào." />;
  }

  const paginatedServices = services.slice((page - 1) * pageSize, page * pageSize);

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: 24 }}>
      <h2 style={{ marginBottom: 16 }}>Bảng giá dịch vụ</h2>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
          gap: 16,
        }}
      >
        {paginatedServices.map((service) => (
          <div
            key={service.id}
            style={{
              border: '1px solid var(--color-border)',
              borderRadius: 8,
              padding: 16,
            }}
          >
            <h4>{service.name}</h4>

            <p
              style={{
                fontSize: 14,
                color: 'var(--color-text-muted)',
                margin: '8px 0',
              }}
            >
              {service.description}
            </p>

            {service.basePrice && (
              <strong style={{ color: 'var(--color-primary)' }}>
                Từ {service.basePrice.toLocaleString('vi-VN')}đ
              </strong>
            )}
          </div>
        ))}
      </div>

      <Pagination page={page} pageSize={pageSize} total={total} onPageChange={setPage} />
    </div>
  );
}
