import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { BookOpen, Search, ArrowRight, Clock, Edit3 } from '../../components/common/FixoraIcons';
import { Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { contentService } from '../../services/contentService';
import type { ArticleListItem } from '../../types/content.types';
import { resolveServiceImageUrl, handleServiceImageError } from '../../utils/imageUrl';
import { extractApiError } from '../../utils/apiError';
import { useAuth } from '../../hooks/useAuth';
import styles from './ArticlesPage.module.css';

const PAGE_SIZE = 9;

export default function ArticlesPage() {
  const { role } = useAuth();
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  useEffect(() => {
    setIsLoading(true);
    contentService
      .getPublicArticles({ page: 1, pageSize: 100 })
      .then((res) => setArticles(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => setIsLoading(false));
  }, []);

  const filteredArticles = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return articles;
    return articles.filter(
      (a) =>
        a.title.toLowerCase().includes(q) ||
        (a.summary || '').toLowerCase().includes(q)
    );
  }, [articles, search]);

  const total = filteredArticles.length;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  useEffect(() => {
    setPage(1);
  }, [search]);

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const featuredArticle = page === 1 && !search.trim() ? filteredArticles[0] : null;
  const gridArticles = useMemo(() => {
    const source = featuredArticle ? filteredArticles.slice(1) : filteredArticles;
    const start = (page - 1) * PAGE_SIZE;
    return source.slice(start, start + PAGE_SIZE);
  }, [filteredArticles, featuredArticle, page]);

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerTop}>
          <div>
            <span className={styles.kicker}>FIXORA JOURNAL · CẨM NANG KỸ THUẬT</span>
            <h1>Kiến thức sửa chữa & bảo quản thiết bị bền bỉ.</h1>
            <p>
              Tổng hợp kinh nghiệm chẩn đoán lỗi phần cứng, mẹo kéo dài tuổi thọ pin và quy trình bảo hành minh bạch từ đội ngũ kỹ thuật viên Fixora.
            </p>
          </div>
          {role === 'Admin' && (
            <Link to="/admin/articles" className={styles.adminShortcut}>
              <Button variant="secondary" size="sm">
                <Edit3 size={15} /> Quản lý bài viết (CRUD)
              </Button>
            </Link>
          )}
        </div>

        <div className={styles.toolbar}>
          <label className={styles.searchBox}>
            <Search size={18} className={styles.searchIcon} />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm kiếm bài viết, mẹo bảo quản iPhone, MacBook, iPad..."
              aria-label="Tìm kiếm bài viết"
            />
            {search && (
              <button
                type="button"
                className={styles.clearBtn}
                onClick={() => setSearch('')}
                aria-label="Xóa từ khóa"
              >
                ×
              </button>
            )}
          </label>
        </div>
      </header>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}

      {!isLoading && !errorMessage && filteredArticles.length === 0 && (
        <EmptyState message="Chưa tìm thấy bài viết nào phù hợp với từ khóa của bạn." />
      )}

      {!isLoading && !errorMessage && filteredArticles.length > 0 && (
        <>
          {featuredArticle && (
            <section className={styles.featuredSection}>
              <Link
                to={`/articles/${featuredArticle.id}`}
                className={styles.featuredCard}
              >
                <div className={styles.featuredImageWrap}>
                  <img
                    src={resolveServiceImageUrl(featuredArticle.imageUrl)}
                    alt={featuredArticle.title}
                    className={styles.featuredImage}
                    onError={handleServiceImageError}
                  />
                  <span className={styles.featuredBadge}>Bài viết tiêu điểm</span>
                </div>
                <div className={styles.featuredBody}>
                  <div className={styles.metaRow}>
                    <span className={styles.categoryTag}>
                      <BookOpen size={13} /> Cẩm nang Fixora
                    </span>
                    <time className={styles.dateText}>
                      <Clock size={13} />{' '}
                      {new Date(featuredArticle.createdAt).toLocaleDateString('vi-VN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                      })}
                    </time>
                  </div>
                  <h2>{featuredArticle.title}</h2>
                  <p>
                    {featuredArticle.summary ||
                      'Khám phá hướng dẫn chi tiết và khuyến nghị kỹ thuật từ chuyên gia sửa chữa phần cứng tại Fixora.'}
                  </p>
                  <span className={styles.readMoreBtn}>
                    Đọc toàn bộ bài viết <ArrowRight size={15} />
                  </span>
                </div>
              </Link>
            </section>
          )}

          {gridArticles.length > 0 && (
            <section className={styles.gridSection}>
              <div className={styles.grid}>
                {gridArticles.map((article) => (
                  <article key={article.id} className={styles.card}>
                    <Link
                      to={`/articles/${article.id}`}
                      className={styles.cardImageLink}
                    >
                      <div className={styles.cardImageWrap}>
                        <img
                          src={resolveServiceImageUrl(article.imageUrl)}
                          alt={article.title}
                          className={styles.cardImage}
                          loading="lazy"
                          onError={handleServiceImageError}
                        />
                      </div>
                    </Link>

                    <div className={styles.cardBody}>
                      <div className={styles.metaRow}>
                        <span className={styles.categoryTag}>
                          <BookOpen size={12} /> Góc kỹ thuật
                        </span>
                        <time className={styles.dateText}>
                          {new Date(article.createdAt).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                          })}
                        </time>
                      </div>

                      <h3>
                        <Link to={`/articles/${article.id}`} className={styles.titleLink}>
                          {article.title}
                        </Link>
                      </h3>

                      <p className={styles.summary}>
                        {article.summary ||
                          'Chia sẻ kinh nghiệm sử dụng, chẩn đoán sớm hư hỏng và bảo vệ thiết bị công nghệ của bạn.'}
                      </p>

                      <Link to={`/articles/${article.id}`} className={styles.cardReadMore}>
                        Xem chi tiết <ArrowRight size={14} />
                      </Link>
                    </div>
                  </article>
                ))}
              </div>
            </section>
          )}

          {total > PAGE_SIZE && (
            <div className={styles.paginationWrap}>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                total={featuredArticle ? total - 1 : total}
                onPageChange={setPage}
              />
            </div>
          )}
        </>
      )}
    </main>
  );
}
