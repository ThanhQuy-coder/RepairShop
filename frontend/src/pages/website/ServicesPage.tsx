import { useEffect, useState } from 'react';
import { contentService } from '../../services/contentService';
import type { ServiceItem } from '../../types/content.types';
import { Loading, EmptyState } from '../../components/common';

export default function ServicesPage() {
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    contentService
      .getPublicServices()
      .then((res) => setServices(res.items))
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) return <Loading />;
  if (services.length === 0) return <EmptyState message="Chưa có dịch vụ nào." />;

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
        {services.map((s) => (
          <div
            key={s.id}
            style={{ border: '1px solid var(--color-border)', borderRadius: 8, padding: 16 }}
          >
            <h4>{s.name}</h4>
            <p style={{ fontSize: 14, color: 'var(--color-text-muted)', margin: '8px 0' }}>
              {s.description}
            </p>
            {s.basePrice && (
              <strong style={{ color: 'var(--color-primary)' }}>
                Từ {s.basePrice.toLocaleString('vi-VN')}đ
              </strong>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
