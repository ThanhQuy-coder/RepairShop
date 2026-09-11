import { useEffect, useState } from 'react';
import { contentService } from '../../services/contentService';
import type { ArticleListItem } from '../../types/content.types';
import { Loading, EmptyState } from '../../components/common';

export default function ArticlesPage() {
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    contentService
      .getPublicArticles()
      .then((res) => setArticles(res.items))
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) return <Loading />;

  if (articles.length === 0) {
    return <EmptyState message="Chưa có bài viết nào." />;
  }

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
        {articles.map((article) => (
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
    </div>
  );
}
