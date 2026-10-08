import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { contentService } from '../../services/contentService';
import { extractApiError } from '../../utils/apiError';
import type { ArticleDetail, ArticleListItem } from '../../types/content.types';
import styles from './ArticleDetailPage.module.css';

export default function ArticleDetailPage() {
  const { id } = useParams();
  const [article, setArticle] = useState<ArticleDetail | null>(null);
  const [related, setRelated] = useState<ArticleListItem[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    Promise.all([contentService.getArticleById(id), contentService.getPublicArticles()])
      .then(([detail, list]) => {
        setArticle(detail);
        setRelated(list.items.filter((item) => item.id !== id).slice(0, 3));
      })
      .catch((error) => setErrorMessage(extractApiError(error).message));
  }, [id]);

  const copyLink = async () => {
    await navigator.clipboard?.writeText(window.location.href);
  };

  if (errorMessage) return <main className={styles.page}><ErrorMessage message={errorMessage} /></main>;
  if (!article) return <main className={styles.page}><Loading /></main>;

  return <main className={styles.page}>
    <Link to="/articles" className={styles.back}>← Tất cả bài viết</Link>
    <article className={styles.article}>
      {article.imageUrl && <img src={article.imageUrl} alt="" className={styles.image} />}
      <span className={styles.kicker}>REPAIRSHOP JOURNAL</span>
      <h1>{article.title}</h1>
      <time dateTime={article.createdAt}>{new Date(article.createdAt).toLocaleDateString('vi-VN')}</time>
      <div className={styles.share}><Button variant="secondary" size="sm" onClick={copyLink}>Sao chép liên kết</Button></div>
      <div className={styles.content}>{article.content.split(/\r?\n/).map((paragraph, index) => paragraph ? <p key={`${paragraph}-${index}`}>{paragraph}</p> : <br key={index} />)}</div>
    </article>
    {related.length > 0 && <section className={styles.related}><h2>Có thể bạn quan tâm</h2><div>{related.map((item) => <Link to={`/articles/${item.id}`} key={item.id}><strong>{item.title}</strong><span>{new Date(item.createdAt).toLocaleDateString('vi-VN')} →</span></Link>)}</div></section>}
  </main>;
}
