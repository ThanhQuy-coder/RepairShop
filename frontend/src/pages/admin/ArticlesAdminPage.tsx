import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Plus,
  Search,
  Edit3,
  Trash2,
  Upload,
  X,
  Eye,
  EyeOff,
  BookOpen,
  Image as ImageIcon,
} from '../../components/common/FixoraIcons';
import {
  Badge,
  Button,
  ConfirmDialog,
  EmptyState,
  ErrorMessage,
  Input,
  Loading,
  Modal,
  Pagination,
  Select,
} from '../../components/common';
import { contentService } from '../../services/contentService';
import type { ArticleListItem } from '../../types/content.types';
import {
  resolveServiceImageUrl,
  handleServiceImageError,
  FIXORA_PLACEHOLDER_IMAGE,
} from '../../utils/imageUrl';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import styles from './ArticlesAdminPage.module.css';

const PAGE_SIZE = 10;
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5MB

export default function ArticlesAdminPage() {
  const { showSuccess, showError } = useToast();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState<'all' | 'published' | 'draft'>('all');
  const [page, setPage] = useState(1);

  // Create / Edit Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [editingArticle, setEditingArticle] = useState<ArticleListItem | null>(null);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [imageUrlInput, setImageUrlInput] = useState('');
  const [isPublished, setIsPublished] = useState(true);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [filePreviewUrl, setFilePreviewUrl] = useState<string | null>(null);
  const [removeCurrentImage, setRemoveCurrentImage] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  // Delete state
  const [deletingArticle, setDeletingArticle] = useState<ArticleListItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const loadArticles = useCallback(async () => {
    setIsLoading(true);
    setPageError(null);
    try {
      const res = await contentService.getAdminArticles({ page: 1, pageSize: 200 });
      setArticles(res.items);
    } catch (err) {
      setPageError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadArticles();
  }, [loadArticles]);

  useEffect(() => {
    return () => {
      if (filePreviewUrl) {
        URL.revokeObjectURL(filePreviewUrl);
      }
    };
  }, [filePreviewUrl]);

  const filteredArticles = useMemo(() => {
    const q = search.trim().toLowerCase();
    return articles.filter((a) => {
      const published = a.isPublished ?? true;
      if (filterStatus === 'published' && !published) return false;
      if (filterStatus === 'draft' && published) return false;
      if (q) {
        const matchTitle = a.title.toLowerCase().includes(q);
        const matchSummary = (a.summary || '').toLowerCase().includes(q);
        return matchTitle || matchSummary;
      }
      return true;
    });
  }, [articles, search, filterStatus]);

  const paginatedArticles = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filteredArticles.slice(start, start + PAGE_SIZE);
  }, [filteredArticles, page]);

  const publishedCount = useMemo(
    () => articles.filter((a) => a.isPublished ?? true).length,
    [articles]
  );

  const openCreateModal = () => {
    setEditingArticle(null);
    setTitle('');
    setContent('');
    setImageUrlInput('');
    setIsPublished(true);
    setSelectedFile(null);
    if (filePreviewUrl) URL.revokeObjectURL(filePreviewUrl);
    setFilePreviewUrl(null);
    setRemoveCurrentImage(false);
    setFormError(null);
    setIsModalOpen(true);
  };

  const openEditModal = async (article: ArticleListItem) => {
    setEditingArticle(article);
    setTitle(article.title);
    setContent(article.summary || '');
    setImageUrlInput(article.imageUrl || '');
    setIsPublished(article.isPublished ?? true);
    setSelectedFile(null);
    if (filePreviewUrl) URL.revokeObjectURL(filePreviewUrl);
    setFilePreviewUrl(null);
    setRemoveCurrentImage(false);
    setFormError(null);
    setIsModalOpen(true);

    setIsLoadingDetail(true);
    try {
      const detail = await contentService.getArticleById(article.id);
      setTitle(detail.title);
      setContent(detail.content);
      setImageUrlInput(detail.imageUrl || '');
      setIsPublished(detail.isPublished);
    } catch (err) {
      setFormError(extractApiError(err).message);
    } finally {
      setIsLoadingDetail(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormError(null);
    const file = e.target.files?.[0];
    if (!file) return;

    const ext = '.' + (file.name.split('.').pop() || '').toLowerCase();
    if (!ALLOWED_IMAGE_TYPES.includes(file.type) || !ALLOWED_EXTENSIONS.includes(ext)) {
      setFormError('Định dạng ảnh không hợp lệ. Chỉ chấp nhận file .jpg, .jpeg, .png, .webp.');
      e.target.value = '';
      return;
    }

    if (file.size > MAX_IMAGE_BYTES) {
      setFormError('Dung lượng ảnh vượt quá giới hạn 5MB. Vui lòng chọn ảnh nhỏ hơn.');
      e.target.value = '';
      return;
    }

    if (filePreviewUrl) {
      URL.revokeObjectURL(filePreviewUrl);
    }
    const preview = URL.createObjectURL(file);
    setSelectedFile(file);
    setFilePreviewUrl(preview);
    setRemoveCurrentImage(false);
  };

  const handleClearSelectedFile = () => {
    if (filePreviewUrl) {
      URL.revokeObjectURL(filePreviewUrl);
    }
    setSelectedFile(null);
    setFilePreviewUrl(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleRemoveExistingImage = () => {
    handleClearSelectedFile();
    setImageUrlInput('');
    setRemoveCurrentImage(true);
  };

  const currentPreviewSrc = useMemo(() => {
    if (filePreviewUrl) return filePreviewUrl;
    if (removeCurrentImage) return FIXORA_PLACEHOLDER_IMAGE;
    if (imageUrlInput.trim()) return resolveServiceImageUrl(imageUrlInput.trim());
    if (editingArticle?.imageUrl) return resolveServiceImageUrl(editingArticle.imageUrl);
    return FIXORA_PLACEHOLDER_IMAGE;
  }, [filePreviewUrl, removeCurrentImage, imageUrlInput, editingArticle]);

  const handleSaveArticle = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!title.trim()) {
      setFormError('Vui lòng nhập tiêu đề bài viết.');
      return;
    }
    if (!content.trim()) {
      setFormError('Vui lòng nhập nội dung bài viết.');
      return;
    }

    setIsSaving(true);
    try {
      let finalImageUrl: string | null = removeCurrentImage
        ? null
        : imageUrlInput.trim() || null;

      if (selectedFile) {
        const uploaded = await contentService.uploadServiceImage(selectedFile);
        finalImageUrl = uploaded.url;
      }

      if (editingArticle) {
        await contentService.updateArticle(editingArticle.id, {
          title: title.trim(),
          content: content.trim(),
          imageUrl: finalImageUrl,
          isPublished,
        });
        showSuccess('Đã cập nhật bài viết thành công.');
      } else {
        await contentService.createArticle({
          title: title.trim(),
          content: content.trim(),
          imageUrl: finalImageUrl,
          isPublished,
        });
        showSuccess('Đã tạo bài viết mới thành công.');
      }

      setIsModalOpen(false);
      await loadArticles();
    } catch (err) {
      setFormError(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const handleTogglePublish = async (article: ArticleListItem) => {
    const nextPublish = !(article.isPublished ?? true);
    try {
      await contentService.toggleArticlePublish(article.id, nextPublish);
      showSuccess(
        nextPublish
          ? `Đã xuất bản bài viết "${article.title}"`
          : `Đã chuyển bài viết "${article.title}" về bản nháp`
      );
      await loadArticles();
    } catch (err) {
      showError(extractApiError(err).message);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deletingArticle) return;
    setIsDeleting(true);
    try {
      await contentService.deleteArticle(deletingArticle.id);
      showSuccess(`Đã xóa bài viết "${deletingArticle.title}"`);
      setDeletingArticle(null);
      await loadArticles();
    } catch (err) {
      showError(extractApiError(err).message);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>Fixora Content Studio</span>
          <h1 className={styles.title}>Quản lý Bài viết & Cẩm nang</h1>
          <p className={styles.subtitle}>
            Biên tập tin tức kỹ thuật, hướng dẫn bảo quản thiết bị và chính sách bảo hành hiển thị trên website Fixora.
          </p>
        </div>

        <div className={styles.headerActions}>
          <div className={styles.statPills}>
            <div className={styles.statPill}>
              <strong>{articles.length}</strong>
              <span>Tổng bài viết</span>
            </div>
            <div className={styles.statPill}>
              <strong>{publishedCount}</strong>
              <span>Đang xuất bản</span>
            </div>
          </div>
          <Button onClick={openCreateModal}>
            <Plus size={16} /> Viết bài mới
          </Button>
        </div>
      </div>

      <div className={styles.toolbarCard}>
        <div className={styles.toolbar}>
          <div className={styles.searchWrap}>
            <Search size={16} className={styles.searchIcon} />
            <input
              type="text"
              className={styles.searchInput}
              placeholder="Tìm theo tiêu đề hoặc tóm tắt bài viết..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className={styles.filters}>
            <Select
              value={filterStatus}
              onChange={(e) => {
                setFilterStatus(e.target.value as 'all' | 'published' | 'draft');
                setPage(1);
              }}
              options={[
                { value: 'all', label: 'Tất cả trạng thái' },
                { value: 'published', label: 'Đã xuất bản' },
                { value: 'draft', label: 'Bản nháp (Chưa đăng)' },
              ]}
            />
          </div>
        </div>

        {pageError && <ErrorMessage message={pageError} />}

        {isLoading ? (
          <div className={styles.loadingWrap}>
            <Loading />
          </div>
        ) : paginatedArticles.length === 0 ? (
          <EmptyState message="Chưa có bài viết nào phù hợp. Hãy nhấn 'Viết bài mới' để bắt đầu chia sẻ cẩm nang kỹ thuật." />
        ) : (
          <>
            <div className={styles.tableWrapper}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Ảnh bìa</th>
                    <th>Tiêu đề & Tóm tắt nội dung</th>
                    <th>Ngày tạo</th>
                    <th>Trạng thái</th>
                    <th className={styles.rightCol}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedArticles.map((article) => {
                    const published = article.isPublished ?? true;
                    return (
                      <tr key={article.id}>
                        <td className={styles.thumbCell}>
                          <div className={styles.thumbBox}>
                            <img
                              src={resolveServiceImageUrl(article.imageUrl)}
                              alt={article.title}
                              className={styles.thumbImg}
                              onError={handleServiceImageError}
                            />
                          </div>
                        </td>
                        <td>
                          <div className={styles.articleTitleRow}>
                            <Link
                              to={`/articles/${article.id}`}
                              target="_blank"
                              rel="noreferrer"
                              className={styles.articleTitle}
                            >
                              {article.title}
                            </Link>
                          </div>
                          <div className={styles.articleSummary}>
                            {article.summary || 'Nhấn Sửa để xem và cập nhật nội dung chi tiết bài viết.'}
                          </div>
                        </td>
                        <td className={styles.dateCell}>
                          {new Date(article.createdAt).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                          })}
                        </td>
                        <td>
                          <Badge variant={published ? 'success' : 'default'}>
                            {published ? 'Đã xuất bản' : 'Bản nháp'}
                          </Badge>
                        </td>
                        <td className={styles.rightCol}>
                          <div className={styles.rowActions}>
                            <Button
                              variant="secondary"
                              size="sm"
                              onClick={() => void openEditModal(article)}
                              title="Chỉnh sửa bài viết"
                            >
                              <Edit3 size={14} /> Sửa
                            </Button>
                            <Button
                              variant="secondary"
                              size="sm"
                              onClick={() => void handleTogglePublish(article)}
                              title={published ? 'Chuyển về bản nháp' : 'Xuất bản lên website'}
                            >
                              {published ? <EyeOff size={14} /> : <Eye size={14} />}
                            </Button>
                            <Button
                              variant="danger"
                              size="sm"
                              onClick={() => setDeletingArticle(article)}
                              title="Xóa bài viết"
                            >
                              <Trash2 size={14} />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <div className={styles.paginationWrap}>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                total={filteredArticles.length}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingArticle ? 'Chỉnh sửa bài viết' : 'Viết bài cẩm nang mới'}
      >
        <form onSubmit={handleSaveArticle} className={styles.modalForm}>
          {formError && <div className={styles.errorAlert}>{formError}</div>}

          <Input
            label="Tiêu đề bài viết *"
            placeholder="VD: 5 dấu hiệu nhận biết pin iPhone cần thay thế ngay"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />

          <label className={styles.publishToggleRow}>
            <input
              type="checkbox"
              checked={isPublished}
              onChange={(e) => setIsPublished(e.target.checked)}
            />
            <div>
              <strong>Xuất bản công khai trên website Fixora</strong>
              <span>Bỏ chọn nếu bạn muốn lưu bài viết này dưới dạng bản nháp.</span>
            </div>
          </label>

          <div className={styles.imageUploadSection}>
            <label className={styles.label}>
              Ảnh bìa bài viết (JPG, PNG, WEBP — tối đa 5MB)
            </label>
            <div className={styles.imagePreviewRow}>
              <div className={styles.previewFrame}>
                <img
                  src={currentPreviewSrc}
                  alt="Article cover preview"
                  className={styles.previewImg}
                  onError={handleServiceImageError}
                />
              </div>
              <div className={styles.uploadControls}>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                  onChange={handleFileChange}
                  className={styles.hiddenFileInput}
                />
                <div className={styles.uploadButtons}>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => fileInputRef.current?.click()}
                  >
                    <Upload size={15} />{' '}
                    {selectedFile ? 'Đổi file ảnh khác' : 'Tải ảnh từ máy'}
                  </Button>
                  {selectedFile && (
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      onClick={handleClearSelectedFile}
                    >
                      <X size={15} /> Hủy ảnh vừa chọn
                    </Button>
                  )}
                  {!selectedFile &&
                    (editingArticle?.imageUrl || imageUrlInput) &&
                    !removeCurrentImage && (
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        onClick={handleRemoveExistingImage}
                      >
                        <ImageIcon size={15} /> Dùng ảnh mặc định Fixora
                      </Button>
                    )}
                </div>

                {selectedFile ? (
                  <p className={styles.fileHint}>
                    Đã chọn: <strong>{selectedFile.name}</strong> (
                    {(selectedFile.size / 1024).toFixed(1)} KB)
                  </p>
                ) : (
                  <Input
                    label="Hoặc nhập đường dẫn ảnh bìa (tùy chọn)"
                    placeholder="/images/services/phone-battery.svg hoặc URL"
                    value={imageUrlInput}
                    onChange={(e) => {
                      setImageUrlInput(e.target.value);
                      setRemoveCurrentImage(false);
                    }}
                  />
                )}
              </div>
            </div>
          </div>

          <div className={styles.textareaGroup}>
            <div className={styles.textareaHeader}>
              <label className={styles.label}>Nội dung chi tiết bài viết *</label>
              <span className={styles.charCount}>
                <BookOpen size={13} /> {content.length} ký tự
              </span>
            </div>
            {isLoadingDetail ? (
              <Loading message="Đang tải nội dung bài viết..." />
            ) : (
              <textarea
                className={styles.textarea}
                rows={10}
                placeholder="Nhập nội dung bài viết. Xuống dòng để tách đoạn văn..."
                value={content}
                onChange={(e) => setContent(e.target.value)}
                required
              />
            )}
          </div>

          <div className={styles.modalFooter}>
            <Button
              type="button"
              variant="secondary"
              onClick={() => setIsModalOpen(false)}
            >
              Hủy
            </Button>
            <Button type="submit" isLoading={isSaving} disabled={isLoadingDetail}>
              {editingArticle ? 'Lưu cập nhật' : 'Đăng bài viết'}
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={Boolean(deletingArticle)}
        onCancel={() => setDeletingArticle(null)}
        onConfirm={() => void handleConfirmDelete()}
        title="Xóa bài viết"
        message={`Bạn có chắc chắn muốn xóa bài viết "${deletingArticle?.title ?? ''}" khỏi hệ thống?`}
        confirmLabel="Xóa bài viết"
        isDangerous
        isLoading={isDeleting}
      />
    </div>
  );
}
