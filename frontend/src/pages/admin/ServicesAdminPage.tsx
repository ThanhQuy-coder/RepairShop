import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Plus,
  Search,
  Edit3,
  Upload,
  X,
  Star,
  Eye,
  EyeOff,
  Image as ImageIcon,
} from '../../components/common/FixoraIcons';
import {
  Badge,
  Button,
  EmptyState,
  ErrorMessage,
  Input,
  Loading,
  Modal,
  Pagination,
  Select,
} from '../../components/common';
import { contentService } from '../../services/contentService';
import type { ServiceItem } from '../../types/content.types';
import { DEVICE_TYPE_OPTIONS, DEVICE_TYPE_LABELS } from '../../types/device.types';
import {
  resolveServiceImageUrl,
  handleServiceImageError,
  FIXORA_PLACEHOLDER_IMAGE,
} from '../../utils/imageUrl';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import styles from './ServicesAdminPage.module.css';

const PAGE_SIZE = 12;
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5MB

const DEVICE_ICONS: Record<string, string> = {
  Phone: '📱',
  Laptop: '💻',
  Tablet: '📟',
  Smartwatch: '⌚',
  Electronics: '🔌',
};

export default function ServicesAdminPage() {
  const { showSuccess, showError } = useToast();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [services, setServices] = useState<ServiceItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  const [search, setSearch] = useState('');
  const [filterDeviceType, setFilterDeviceType] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const [viewMode, setViewMode] = useState<'table' | 'grid'>('table');
  const [page, setPage] = useState(1);

  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<ServiceItem | null>(null);
  const [name, setName] = useState('');
  const [deviceType, setDeviceType] = useState<string>('Phone');
  const [basePrice, setBasePrice] = useState<string>('');
  const [description, setDescription] = useState('');
  const [imageUrlInput, setImageUrlInput] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [filePreviewUrl, setFilePreviewUrl] = useState<string | null>(null);
  const [removeCurrentImage, setRemoveCurrentImage] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const loadServices = useCallback(async () => {
    setIsLoading(true);
    setPageError(null);
    try {
      const res = await contentService.getAdminServices({ page: 1, pageSize: 200 });
      setServices(res.items);
    } catch (err) {
      setPageError(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadServices();
  }, [loadServices]);

  useEffect(() => {
    return () => {
      if (filePreviewUrl) {
        URL.revokeObjectURL(filePreviewUrl);
      }
    };
  }, [filePreviewUrl]);

  const stats = useMemo(() => {
    const activeCount = services.filter((s) => s.isActive).length;
    const inactiveCount = services.length - activeCount;
    const ratedServices = services.filter((s) => (s.reviewCount ?? 0) > 0);
    const totalReviews = services.reduce((acc, s) => acc + (s.reviewCount ?? 0), 0);
    const avgRating =
      ratedServices.length > 0
        ? (
            ratedServices.reduce(
              (acc, s) => acc + (s.averageRating ?? 0) * (s.reviewCount ?? 0),
              0
            ) / Math.max(1, totalReviews)
          ).toFixed(1)
        : '—';
    return { total: services.length, activeCount, inactiveCount, totalReviews, avgRating };
  }, [services]);

  const filteredServices = useMemo(() => {
    const q = search.trim().toLowerCase();
    return services.filter((s) => {
      if (
        filterDeviceType &&
        (s.deviceType || '').toLowerCase() !== filterDeviceType.toLowerCase()
      ) {
        return false;
      }
      if (filterStatus === 'active' && !s.isActive) return false;
      if (filterStatus === 'inactive' && s.isActive) return false;
      if (q) {
        const matchName = s.name.toLowerCase().includes(q);
        const matchDesc = (s.description || '').toLowerCase().includes(q);
        return matchName || matchDesc;
      }
      return true;
    });
  }, [services, search, filterDeviceType, filterStatus]);

  const paginatedServices = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filteredServices.slice(start, start + PAGE_SIZE);
  }, [filteredServices, page]);

  const openCreateModal = () => {
    setEditingService(null);
    setName('');
    setDeviceType(filterDeviceType || 'Phone');
    setBasePrice('');
    setDescription('');
    setImageUrlInput('');
    setSelectedFile(null);
    if (filePreviewUrl) URL.revokeObjectURL(filePreviewUrl);
    setFilePreviewUrl(null);
    setRemoveCurrentImage(false);
    setFormError(null);
    setIsModalOpen(true);
  };

  const openEditModal = (service: ServiceItem) => {
    setEditingService(service);
    setName(service.name);
    setDeviceType(service.deviceType || 'Phone');
    setBasePrice(service.basePrice !== null ? String(service.basePrice) : '');
    setDescription(service.description || '');
    setImageUrlInput(service.imageUrl || '');
    setSelectedFile(null);
    if (filePreviewUrl) URL.revokeObjectURL(filePreviewUrl);
    setFilePreviewUrl(null);
    setRemoveCurrentImage(false);
    setFormError(null);
    setIsModalOpen(true);
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
    if (imageUrlInput.trim()) return resolveServiceImageUrl(imageUrlInput.trim(), deviceType);
    if (editingService?.imageUrl)
      return resolveServiceImageUrl(editingService.imageUrl, deviceType);
    return FIXORA_PLACEHOLDER_IMAGE;
  }, [filePreviewUrl, removeCurrentImage, imageUrlInput, editingService, deviceType]);

  const handleSaveService = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!name.trim()) {
      setFormError('Vui lòng nhập tên dịch vụ sửa chữa.');
      return;
    }

    const parsedPrice = basePrice.trim() ? Number(basePrice) : null;
    if (parsedPrice !== null && (Number.isNaN(parsedPrice) || parsedPrice < 0)) {
      setFormError('Giá dịch vụ tham khảo phải là số không âm.');
      return;
    }

    if (!description.trim()) {
      setFormError('Vui lòng nhập mô tả dịch vụ.');
      return;
    }

    setIsSaving(true);
    try {
      if (editingService) {
        await contentService.updateService(editingService.id, {
          name: name.trim(),
          description: description.trim(),
          basePrice: parsedPrice,
          deviceType: deviceType || null,
          imageUrl:
            !selectedFile && !removeCurrentImage ? imageUrlInput.trim() || null : null,
          removeImage: removeCurrentImage,
          imageFile: selectedFile,
        });
        showSuccess('Đã cập nhật dịch vụ sửa chữa');
      } else {
        await contentService.createService({
          name: name.trim(),
          description: description.trim(),
          basePrice: parsedPrice,
          deviceType: deviceType || null,
          imageUrl: !selectedFile ? imageUrlInput.trim() || null : null,
          imageFile: selectedFile,
        });
        showSuccess('Đã tạo dịch vụ sửa chữa mới');
      }
      setIsModalOpen(false);
      await loadServices();
    } catch (err) {
      setFormError(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleStatus = async (service: ServiceItem) => {
    try {
      await contentService.toggleServicePublish(service.id, !service.isActive);
      showSuccess(
        service.isActive
          ? `Đã tạm ẩn dịch vụ "${service.name}"`
          : `Đã kích hoạt dịch vụ "${service.name}"`
      );
      await loadServices();
    } catch (err) {
      showError(extractApiError(err).message);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>DANH MỤC DỊCH VỤ — FIXORA</span>
          <h1 className={styles.title}>Quản lý Dịch vụ Sửa chữa</h1>
          <p className={styles.subtitle}>
            Quản lý danh mục dịch vụ, bảng giá tham khảo, phân loại thiết bị và hình ảnh hiển thị trên website Fixora.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          <Plus size={16} /> Thêm dịch vụ mới
        </Button>
      </div>

      {/* 4 Thẻ KPI tổng quan danh mục dịch vụ */}
      <div className={styles.kpiGrid}>
        <div
          className={`${styles.kpiCard} ${filterStatus === '' ? styles.kpiCardActive : ''}`}
          onClick={() => {
            setFilterStatus('');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>🛠️</span>
          <div>
            <strong>{stats.total}</strong>
            <span>Tổng dịch vụ hệ thống</span>
          </div>
        </div>
        <div
          className={`${styles.kpiCard} ${
            filterStatus === 'active' ? styles.kpiCardActive : ''
          }`}
          onClick={() => {
            setFilterStatus(filterStatus === 'active' ? '' : 'active');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>👁️</span>
          <div>
            <strong>{stats.activeCount}</strong>
            <span>Đang hiển thị trên web</span>
          </div>
        </div>
        <div
          className={`${styles.kpiCard} ${
            filterStatus === 'inactive' ? styles.kpiCardActive : ''
          }`}
          onClick={() => {
            setFilterStatus(filterStatus === 'inactive' ? '' : 'inactive');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>⏸️</span>
          <div>
            <strong>{stats.inactiveCount}</strong>
            <span>Đang tạm ẩn</span>
          </div>
        </div>
        <div className={styles.kpiCard}>
          <span className={styles.kpiIcon}>⭐</span>
          <div>
            <strong>
              {stats.avgRating} <small>({stats.totalReviews} ĐG)</small>
            </strong>
            <span>Đánh giá dịch vụ TB</span>
          </div>
        </div>
      </div>

      {/* Thanh pill chọn nhanh theo loại thiết bị */}
      <div className={styles.deviceTabsBar}>
        <button
          type="button"
          className={`${styles.deviceTab} ${
            filterDeviceType === '' ? styles.deviceTabActive : ''
          }`}
          onClick={() => {
            setFilterDeviceType('');
            setPage(1);
          }}
        >
          <span>Tất cả thiết bị</span>
          <strong>{services.length}</strong>
        </button>
        {DEVICE_TYPE_OPTIONS.map((opt) => {
          const count = services.filter(
            (s) => (s.deviceType || '').toLowerCase() === opt.value.toLowerCase()
          ).length;
          return (
            <button
              key={opt.value}
              type="button"
              className={`${styles.deviceTab} ${
                filterDeviceType === opt.value ? styles.deviceTabActive : ''
              }`}
              onClick={() => {
                setFilterDeviceType(filterDeviceType === opt.value ? '' : opt.value);
                setPage(1);
              }}
            >
              <span>
                {DEVICE_ICONS[opt.value] ?? '📱'} {DEVICE_TYPE_LABELS[opt.value]}
              </span>
              <strong>{count}</strong>
            </button>
          );
        })}
      </div>

      <div className={styles.toolbarCard}>
        <div className={styles.toolbar}>
          <div className={styles.searchWrap}>
            <Search size={16} className={styles.searchIcon} />
            <input
              type="text"
              className={styles.searchInput}
              placeholder="Tìm nhanh theo tên dịch vụ hoặc nội dung mô tả..."
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
                setFilterStatus(e.target.value);
                setPage(1);
              }}
              options={[
                { value: '', label: 'Tất cả trạng thái' },
                { value: 'active', label: 'Đang hiển thị' },
                { value: 'inactive', label: 'Đang tạm ẩn' },
              ]}
            />
            <div className={styles.viewToggle}>
              <button
                type="button"
                className={`${styles.viewBtn} ${viewMode === 'table' ? styles.viewBtnActive : ''}`}
                onClick={() => setViewMode('table')}
              >
                ☰ Bảng
              </button>
              <button
                type="button"
                className={`${styles.viewBtn} ${viewMode === 'grid' ? styles.viewBtnActive : ''}`}
                onClick={() => setViewMode('grid')}
              >
                ▦ Thẻ ảnh
              </button>
            </div>
          </div>
        </div>

        {pageError && <ErrorMessage message={pageError} />}

        {isLoading ? (
          <div className={styles.loadingWrap}>
            <Loading />
          </div>
        ) : paginatedServices.length === 0 ? (
          <EmptyState message="Chưa tìm thấy dịch vụ phù hợp. Thử thay đổi bộ lọc hoặc thêm dịch vụ mới." />
        ) : viewMode === 'grid' ? (
          <>
            <div className={styles.serviceCardGrid}>
              {paginatedServices.map((service) => (
                <article key={service.id} className={styles.serviceCard}>
                  <div className={styles.cardThumbWrap}>
                    <img
                      src={resolveServiceImageUrl(service.imageUrl, service.deviceType)}
                      alt={service.name}
                      className={styles.cardThumbImg}
                      onError={handleServiceImageError}
                    />
                    <div className={styles.cardTopBadges}>
                      <Badge variant="info">
                        {service.deviceType
                          ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
                          : 'Đa thiết bị'}
                      </Badge>
                      <Badge variant={service.isActive ? 'success' : 'default'}>
                        {service.isActive ? 'Hiển thị' : 'Tạm ẩn'}
                      </Badge>
                    </div>
                  </div>
                  <div className={styles.cardBody}>
                    <h3 className={styles.cardTitle}>{service.name}</h3>
                    <p className={styles.cardDesc}>{service.description}</p>
                    <div className={styles.cardPriceRow}>
                      <span className={styles.priceText}>
                        {service.basePrice !== null
                          ? `${service.basePrice.toLocaleString('vi-VN')}đ`
                          : 'Liên hệ báo giá'}
                      </span>
                      {service.reviewCount && service.reviewCount > 0 ? (
                        <div className={styles.ratingBadge}>
                          <Star size={14} className={styles.starIcon} />
                          <strong>{(service.averageRating ?? 0).toFixed(1)}</strong>
                          <span>({service.reviewCount})</span>
                        </div>
                      ) : (
                        <span className={styles.noRating}>Chưa có ĐG</span>
                      )}
                    </div>
                    <div className={styles.cardFooterActions}>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => openEditModal(service)}
                      >
                        <Edit3 size={14} /> Chỉnh sửa
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => handleToggleStatus(service)}
                      >
                        {service.isActive ? (
                          <>
                            <EyeOff size={14} /> Ẩn
                          </>
                        ) : (
                          <>
                            <Eye size={14} /> Hiện
                          </>
                        )}
                      </Button>
                    </div>
                  </div>
                </article>
              ))}
            </div>
            <div className={styles.paginationWrap}>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                total={filteredServices.length}
                onPageChange={setPage}
              />
            </div>
          </>
        ) : (
          <>
            <div className={styles.tableWrapper}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Hình ảnh</th>
                    <th>Tên dịch vụ &amp; Mô tả</th>
                    <th>Thiết bị</th>
                    <th>Giá tham khảo</th>
                    <th>Đánh giá</th>
                    <th>Trạng thái</th>
                    <th className={styles.rightCol}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedServices.map((service) => (
                    <tr key={service.id}>
                      <td className={styles.thumbCell}>
                        <div className={styles.thumbBox}>
                          <img
                            src={resolveServiceImageUrl(service.imageUrl, service.deviceType)}
                            alt={service.name}
                            className={styles.thumbImg}
                            onError={handleServiceImageError}
                          />
                        </div>
                      </td>
                      <td>
                        <div className={styles.serviceName}>{service.name}</div>
                        <div className={styles.serviceDesc}>{service.description}</div>
                      </td>
                      <td>
                        <Badge variant="info">
                          {service.deviceType
                            ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
                            : 'Đa thiết bị'}
                        </Badge>
                      </td>
                      <td>
                        <span className={styles.priceText}>
                          {service.basePrice !== null
                            ? `${service.basePrice.toLocaleString('vi-VN')}đ`
                            : 'Liên hệ'}
                        </span>
                      </td>
                      <td>
                        {service.reviewCount && service.reviewCount > 0 ? (
                          <div className={styles.ratingBadge}>
                            <Star size={14} className={styles.starIcon} />
                            <strong>{(service.averageRating ?? 0).toFixed(1)}</strong>
                            <span>({service.reviewCount})</span>
                          </div>
                        ) : (
                          <span className={styles.noRating}>Chưa có ĐG</span>
                        )}
                      </td>
                      <td>
                        <Badge variant={service.isActive ? 'success' : 'default'}>
                          {service.isActive ? 'Hiển thị' : 'Tạm ẩn'}
                        </Badge>
                      </td>
                      <td className={styles.rightCol}>
                        <div className={styles.rowActions}>
                          <Button
                            variant="secondary"
                            size="sm"
                            onClick={() => openEditModal(service)}
                            title="Chỉnh sửa dịch vụ"
                          >
                            <Edit3 size={14} /> Sửa
                          </Button>
                          <Button
                            variant="secondary"
                            size="sm"
                            onClick={() => handleToggleStatus(service)}
                            title={
                              service.isActive
                                ? 'Tạm ẩn khỏi website'
                                : 'Hiển thị lên website'
                            }
                          >
                            {service.isActive ? <EyeOff size={14} /> : <Eye size={14} />}
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className={styles.paginationWrap}>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                total={filteredServices.length}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingService ? 'Chỉnh sửa dịch vụ sửa chữa' : 'Thêm dịch vụ sửa chữa mới'}
      >
        <form onSubmit={handleSaveService} className={styles.modalForm}>
          {formError && <div className={styles.errorAlert}>{formError}</div>}

          <Input
            label="Tên dịch vụ *"
            placeholder="VD: Thay màn hình iPhone chính hãng"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />

          <div className={styles.formRow}>
            <Select
              label="Loại thiết bị *"
              value={deviceType}
              onChange={(e) => setDeviceType(e.target.value)}
              options={DEVICE_TYPE_OPTIONS}
            />
            <Input
              label="Giá tham khảo (VNĐ) *"
              type="number"
              min={0}
              step={1000}
              placeholder="VD: 1250000"
              value={basePrice}
              onChange={(e) => setBasePrice(e.target.value)}
              required
            />
          </div>

          <div className={styles.textareaGroup}>
            <label className={styles.label}>Mô tả chi tiết dịch vụ *</label>
            <textarea
              className={styles.textarea}
              rows={3}
              placeholder="Mô tả quy trình, linh kiện thay thế và cam kết bảo hành..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
            />
          </div>

          <div className={styles.imageUploadSection}>
            <label className={styles.label}>
              Hình ảnh dịch vụ (JPG, PNG, WEBP — tối đa 5MB)
            </label>
            <div className={styles.imagePreviewRow}>
              <div className={styles.previewFrame}>
                <img
                  src={currentPreviewSrc}
                  alt="Service preview"
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
                    (editingService?.imageUrl || imageUrlInput) &&
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
                    label="Hoặc nhập đường dẫn ảnh (tùy chọn)"
                    placeholder="/images/services/phone-screen.svg hoặc URL"
                    value={imageUrlInput}
                    onChange={(e) => {
                      setImageUrlInput(e.target.value);
                      setRemoveCurrentImage(false);
                    }}
                  />
                )}
                <span className={styles.helperNote}>
                  Nếu không tải ảnh mới khi chỉnh sửa, hệ thống sẽ giữ nguyên ảnh hiện tại.
                </span>
              </div>
            </div>
          </div>

          <div className={styles.modalFooter}>
            <Button
              type="button"
              variant="secondary"
              onClick={() => setIsModalOpen(false)}
            >
              Hủy
            </Button>
            <Button type="submit" isLoading={isSaving}>
              {editingService ? 'Lưu thay đổi' : 'Tạo dịch vụ'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
