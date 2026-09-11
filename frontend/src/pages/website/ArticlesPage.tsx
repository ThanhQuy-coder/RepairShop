import { useEffect, useState } from 'react';

import { contentService } from '../../services/contentService';
import type { ArticleListItem } from '../../types/content.types';

import { EmptyState, Loading, Pagination } from '../../components/common';

export default function ArticlesPage() {
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);

  const pageSize = 10;

  useEffect(() => {
    contentService
      .getPublicArticles()
      .then((res) => setArticles(res.items))
      .finally(() => setIsLoading(false));
  }, []);

  const total = articles.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  if (isLoading) return <Loading />;

  if (articles.length === 0) {
    return <EmptyState message="Chưa có bài viết nào." />;
  }

  const paginatedArticles = articles.slice((page - 1) * pageSize, page * pageSize);

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: 24 }}>
      <h2 style={{ marginBottom: 16 }}>Bài viết</h2>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
          gap: 16,
        }}
      >
        {paginatedArticles.map((article) => (
          <article
            key={article.id}
            style={{
              border: '1px solid var(--color-border)',
              borderRadius: 8,
              padding: 16,
            }}
          >
            {article.imageUrl && (
              <img
                src={article.imageUrl}
                alt={article.title}
                style={{
                  width: '100%',
                  height: 160,
                  objectFit: 'cover',
                  borderRadius: 6,
                  marginBottom: 12,
                }}
              />
            )}

            <h4>{article.title}</h4>

            <small style={{ color: 'var(--color-text-muted)' }}>
              {new Date(article.createdAt).toLocaleDateString('vi-VN')}
            </small>
          </article>
        ))}
      </div>

      <Pagination page={page} pageSize={pageSize} total={total} onPageChange={setPage} />
    </div>
  );
}
