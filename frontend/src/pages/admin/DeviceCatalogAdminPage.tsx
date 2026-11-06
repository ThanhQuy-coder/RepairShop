import { type FormEvent, useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Badge,
  Button,
  ConfirmDialog,
  EmptyState,
  ErrorMessage,
  Loading,
  Pagination,
} from '../../components/common';
import { useToast } from '../../hooks/useToast';
import { deviceService } from '../../services/deviceService';
import {
  DEVICE_TYPE_LABELS,
  DEVICE_TYPE_OPTIONS,
  type BrandItem,
  type DeviceModelItem,
  type DeviceType,
} from '../../types/device.types';
import { extractApiError } from '../../utils/apiError';
import styles from './DeviceCatalogAdminPage.module.css';

const PAGE_SIZE = 10;

export default function DeviceCatalogAdminPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();

  const initialTab = location.pathname.includes('device-models') ? 'models' : 'brands';
  const [activeTab, setActiveTab] = useState<'brands' | 'models'>(initialTab);

  useEffect(() => {
    setActiveTab(location.pathname.includes('device-models') ? 'models' : 'brands');
  }, [location.pathname]);

  const switchTab = (tab: 'brands' | 'models') => {
    setActiveTab(tab);
    navigate(tab === 'brands' ? '/admin/brands' : '/admin/device-models', { replace: true });
  };

  // ==================== BRANDS STATE ====================
  const [brands, setBrands] = useState<BrandItem[]>([]);
  const [brandsTotal, setBrandsTotal] = useState(0);
  const [brandPage, setBrandPage] = useState(1);
  const [brandSearch, setBrandSearch] = useState('');
  const [brandDeviceTypeFilter, setBrandDeviceTypeFilter] = useState('');
  const [brandStatusFilter, setBrandStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [loadingBrands, setLoadingBrands] = useState(true);
  const [brandError, setBrandError] = useState<string | null>(null);

  const [editingBrand, setEditingBrand] = useState<BrandItem | null>(null);
  const [brandForm, setBrandForm] = useState({
    name: '',
    code: '',
    logoUrl: '',
    description: '',
    isActive: true,
  });
  const [savingBrand, setSavingBrand] = useState(false);
  const [deletingBrand, setDeletingBrand] = useState<BrandItem | null>(null);
  const [isDeletingBrand, setIsDeletingBrand] = useState(false);

  // ==================== DEVICE MODELS STATE ====================
  const [models, setModels] = useState<DeviceModelItem[]>([]);
  const [modelsTotal, setModelsTotal] = useState(0);
  const [modelPage, setModelPage] = useState(1);
  const [modelSearch, setModelSearch] = useState('');
  const [modelDeviceTypeFilter, setModelDeviceTypeFilter] = useState('');
  const [modelBrandFilter, setModelBrandFilter] = useState('');
  const [modelStatusFilter, setModelStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [loadingModels, setLoadingModels] = useState(true);
  const [modelError, setModelError] = useState<string | null>(null);

  // Brands available for the current modelDeviceTypeFilter (cascading filter)
  const [filterBrandOptions, setFilterBrandOptions] = useState<BrandItem[]>([]);
  const [allActiveBrands, setAllActiveBrands] = useState<BrandItem[]>([]);

  const [editingModel, setEditingModel] = useState<DeviceModelItem | null>(null);
  const [modelForm, setModelForm] = useState({
    deviceType: 'Phone' as DeviceType,
    brandId: '',
    modelName: '',
    modelNumber: '',
    releaseYear: '',
    isActive: true,
  });
  const [savingModel, setSavingModel] = useState(false);
  const [deletingModel, setDeletingModel] = useState<DeviceModelItem | null>(null);
  const [isDeletingModel, setIsDeletingModel] = useState(false);

  // ==================== LOAD BRANDS ====================
  const loadBrands = useCallback(async () => {
    setLoadingBrands(true);
    setBrandError(null);
    try {
      const res = await deviceService.getAdminBrands({
        deviceType: brandDeviceTypeFilter || undefined,
        isActive: brandStatusFilter === 'all' ? undefined : brandStatusFilter === 'active',
        search: brandSearch.trim() || undefined,
        page: brandPage,
        pageSize: PAGE_SIZE,
      });
      setBrands(res.items);
      setBrandsTotal(res.total);
    } catch (err) {
      setBrandError(extractApiError(err).message);
    } finally {
      setLoadingBrands(false);
    }
  }, [brandDeviceTypeFilter, brandStatusFilter, brandSearch, brandPage]);

  useEffect(() => {
    loadBrands();
  }, [loadBrands]);

  // Load all active brands for model form dropdown
  const loadAllActiveBrands = useCallback(async () => {
    try {
      const res = await deviceService.getAdminBrands({ isActive: true, pageSize: 100 });
      setAllActiveBrands(res.items);
      if (!modelForm.brandId && res.items.length > 0) {
        setModelForm((prev) => ({ ...prev, brandId: prev.brandId || res.items[0].id }));
      }
    } catch {
      /* ignore */
    }
  }, [modelForm.brandId]);

  useEffect(() => {
    loadAllActiveBrands();
  }, [loadAllActiveBrands]);

  // Cascading Brand options for Model filter bar: only show Brands that have models of modelDeviceTypeFilter
  useEffect(() => {
    let cancelled = false;
    deviceService
      .getAdminBrands({
        deviceType: modelDeviceTypeFilter || undefined,
        pageSize: 100,
      })
      .then((res) => {
        if (!cancelled) {
          setFilterBrandOptions(res.items);
        }
      })
      .catch(() => {
        if (!cancelled) setFilterBrandOptions([]);
      });
    return () => {
      cancelled = true;
    };
  }, [modelDeviceTypeFilter]);

  // ==================== LOAD MODELS ====================
  const loadModels = useCallback(async () => {
    setLoadingModels(true);
    setModelError(null);
    try {
      const res = await deviceService.getAdminModels({
        deviceType: modelDeviceTypeFilter || undefined,
        brandId: modelBrandFilter || undefined,
        isActive: modelStatusFilter === 'all' ? undefined : modelStatusFilter === 'active',
        search: modelSearch.trim() || undefined,
        page: modelPage,
        pageSize: PAGE_SIZE,
      });
      setModels(res.items);
      setModelsTotal(res.total);
    } catch (err) {
      setModelError(extractApiError(err).message);
    } finally {
      setLoadingModels(false);
    }
  }, [modelDeviceTypeFilter, modelBrandFilter, modelStatusFilter, modelSearch, modelPage]);

  useEffect(() => {
    loadModels();
  }, [loadModels]);

  // ==================== BRAND HANDLERS ====================
  const resetBrandForm = () => {
    setEditingBrand(null);
    setBrandForm({ name: '', code: '', logoUrl: '', description: '', isActive: true });
    setBrandError(null);
  };

  const handleBrandSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setBrandError(null);

    if (!brandForm.name.trim() || !brandForm.code.trim()) {
      setBrandError('Vui lòng nhập đầy đủ Tên hãng và Mã hãng sản xuất.');
      return;
    }

    setSavingBrand(true);
    try {
      const payload = {
        name: brandForm.name.trim(),
        code: brandForm.code.trim().toUpperCase(),
        logoUrl: brandForm.logoUrl.trim() || null,
        description: brandForm.description.trim() || null,
        isActive: brandForm.isActive,
      };

      if (editingBrand) {
        await deviceService.updateBrand(editingBrand.id, payload);
        showSuccess('Đã cập nhật hãng sản xuất.');
      } else {
        await deviceService.createBrand(payload);
        showSuccess('Đã thêm hãng sản xuất mới.');
      }
      resetBrandForm();
      await loadBrands();
      await loadAllActiveBrands();
    } catch (err) {
      setBrandError(extractApiError(err).message);
    } finally {
      setSavingBrand(false);
    }
  };

  const handleToggleBrand = async (brand: BrandItem) => {
    try {
      await deviceService.toggleBrandActive(brand.id, !brand.isActive);
      showSuccess(brand.isActive ? 'Đã ngưng kích hoạt hãng.' : 'Đã kích hoạt hãng sản xuất.');
      await loadBrands();
      await loadAllActiveBrands();
    } catch (err) {
      showError(extractApiError(err).message);
    }
  };

  const handleDeleteBrand = async () => {
    if (!deletingBrand) return;
    setIsDeletingBrand(true);
    try {
      await deviceService.deleteBrand(deletingBrand.id);
      showSuccess('Đã xóa hãng sản xuất.');
      if (editingBrand?.id === deletingBrand.id) resetBrandForm();
      setDeletingBrand(null);
      await loadBrands();
      await loadAllActiveBrands();
    } catch (err) {
      showError(extractApiError(err).message);
      setDeletingBrand(null);
    } finally {
      setIsDeletingBrand(false);
    }
  };

  // ==================== MODEL HANDLERS ====================
  const resetModelForm = () => {
    setEditingModel(null);
    setModelForm({
      deviceType: 'Phone',
      brandId: allActiveBrands[0]?.id ?? '',
      modelName: '',
      modelNumber: '',
      releaseYear: '',
      isActive: true,
    });
    setModelError(null);
  };

  const handleModelSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setModelError(null);

    if (!modelForm.brandId || !modelForm.deviceType || !modelForm.modelName.trim()) {
      setModelError('Vui lòng chọn Hãng sản xuất, Loại thiết bị và nhập Tên dòng máy.');
      return;
    }

    const parsedYear = modelForm.releaseYear.trim() ? Number(modelForm.releaseYear.trim()) : null;
    if (parsedYear !== null && (Number.isNaN(parsedYear) || parsedYear < 1990 || parsedYear > 2100)) {
      setModelError('Năm phát hành phải nằm trong khoảng 1990 – 2100.');
      return;
    }

    setSavingModel(true);
    try {
      const payload = {
        brandId: modelForm.brandId,
        deviceType: modelForm.deviceType,
        modelName: modelForm.modelName.trim(),
        modelNumber: modelForm.modelNumber.trim() || null,
        releaseYear: parsedYear,
        isActive: modelForm.isActive,
      };

      if (editingModel) {
        await deviceService.updateDeviceModel(editingModel.id, payload);
        showSuccess('Đã cập nhật dòng máy.');
      } else {
        await deviceService.createDeviceModel(payload);
        showSuccess('Đã tạo dòng máy mới.');
      }
      resetModelForm();
      await loadModels();
      await loadBrands();
    } catch (err) {
      setModelError(extractApiError(err).message);
    } finally {
      setSavingModel(false);
    }
  };

  const handleToggleModel = async (model: DeviceModelItem) => {
    try {
      await deviceService.toggleDeviceModelActive(model.id, !model.isActive);
      showSuccess(model.isActive ? 'Đã ngưng kích hoạt dòng máy.' : 'Đã kích hoạt dòng máy.');
      await loadModels();
    } catch (err) {
      showError(extractApiError(err).message);
    }
  };

  const handleDeleteModel = async () => {
    if (!deletingModel) return;
    setIsDeletingModel(true);
    try {
      await deviceService.deleteDeviceModel(deletingModel.id);
      showSuccess('Đã xóa dòng máy.');
      if (editingModel?.id === deletingModel.id) resetModelForm();
      setDeletingModel(null);
      await loadModels();
      await loadBrands();
    } catch (err) {
      showError(extractApiError(err).message);
      setDeletingModel(null);
    } finally {
      setIsDeletingModel(false);
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>FIXORA • DEVICE CATALOG</span>
          <h1 className={styles.title}>Danh mục Thiết bị & Hãng sản xuất</h1>
          <p className={styles.subtitle}>
            Quản lý cấu trúc phân tầng <strong>DeviceType → Brand → DeviceModel</strong> phục vụ đặt lịch, tiếp nhận và báo giá.
          </p>
        </div>
        <div className={styles.summaryRow}>
          <div className={styles.summaryCard}>
            <strong>{brandsTotal}</strong>
            <span>Hãng sản xuất</span>
          </div>
          <div className={styles.summaryCard}>
            <strong>{modelsTotal}</strong>
            <span>Dòng máy (Model)</span>
          </div>
        </div>
      </header>

      <div className={styles.tabs} role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'brands'}
          className={`${styles.tabBtn} ${activeTab === 'brands' ? styles.tabBtnActive : ''}`}
          onClick={() => switchTab('brands')}
        >
          1. Hãng sản xuất (Brands)
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'models'}
          className={`${styles.tabBtn} ${activeTab === 'models' ? styles.tabBtnActive : ''}`}
          onClick={() => switchTab('models')}
        >
          2. Dòng máy (Device Models)
        </button>
      </div>

      {activeTab === 'brands' ? (
        <>
          <form className={styles.editor} onSubmit={handleBrandSubmit}>
            <div className={styles.sectionHeading}>
              <div>
                <span className={styles.sectionKicker}>
                  {editingBrand ? 'CHỈNH SỬA HÃNG' : 'TẠO HÃNG SẢN XUẤT MỚI'}
                </span>
                <h2>{editingBrand ? `Cập nhật: ${editingBrand.name}` : 'Thông tin hãng sản xuất'}</h2>
              </div>
              {editingBrand && (
                <Button type="button" variant="ghost" size="sm" onClick={resetBrandForm}>
                  Hủy chỉnh sửa
                </Button>
              )}
            </div>

            {brandError && <ErrorMessage message={brandError} />}

            <div className={styles.formGrid3}>
              <label className={styles.field}>
                <span>
                  Tên hãng sản xuất <em>*</em>
                </span>
                <input
                  value={brandForm.name}
                  onChange={(e) => {
                    const name = e.target.value;
                    setBrandForm((prev) => ({
                      ...prev,
                      name,
                      code:
                        !editingBrand && !prev.code
                          ? name
                              .toUpperCase()
                              .replace(/[^A-Z0-9]+/g, '')
                              .slice(0, 20)
                          : prev.code,
                    }));
                  }}
                  placeholder="Ví dụ: Apple, Samsung, Dell..."
                  maxLength={100}
                  required
                />
              </label>

              <label className={styles.field}>
                <span>
                  Mã định danh (Code) <em>*</em>
                </span>
                <input
                  value={brandForm.code}
                  onChange={(e) =>
                    setBrandForm((prev) => ({ ...prev, code: e.target.value.toUpperCase() }))
                  }
                  placeholder="Ví dụ: APPLE, SAMSUNG..."
                  maxLength={50}
                  required
                />
              </label>

              <label className={styles.field}>
                <span>Đường dẫn Logo (Tùy chọn)</span>
                <input
                  value={brandForm.logoUrl}
                  onChange={(e) => setBrandForm((prev) => ({ ...prev, logoUrl: e.target.value }))}
                  placeholder="/images/brands/apple.svg hoặc https://..."
                  maxLength={500}
                />
              </label>

              <label className={`${styles.field} ${styles.fullField}`}>
                <span>Mô tả phạm vi thiết bị</span>
                <input
                  value={brandForm.description}
                  onChange={(e) =>
                    setBrandForm((prev) => ({ ...prev, description: e.target.value }))
                  }
                  placeholder="Mô tả ngắn các dòng sản phẩm chính của hãng..."
                  maxLength={500}
                />
              </label>
            </div>

            <div className={styles.formActions}>
              <Button type="submit" isLoading={savingBrand}>
                {editingBrand ? 'Lưu thay đổi' : 'Thêm hãng sản xuất'}
              </Button>
              <Button type="button" variant="secondary" onClick={resetBrandForm}>
                Làm mới
              </Button>
            </div>
          </form>

          <section className={styles.catalog}>
            <div className={styles.catalogHeader}>
              <div>
                <span className={styles.sectionKicker}>DANH MỤC HÃNG</span>
                <h2>
                  Danh sách hãng sản xuất <span>({brandsTotal})</span>
                </h2>
              </div>

              <div className={styles.filters}>
                <label className={styles.search}>
                  <span>⌕</span>
                  <input
                    value={brandSearch}
                    onChange={(e) => {
                      setBrandSearch(e.target.value);
                      setBrandPage(1);
                    }}
                    placeholder="Tìm theo tên hoặc mã hãng..."
                  />
                </label>

                <select
                  value={brandDeviceTypeFilter}
                  onChange={(e) => {
                    setBrandDeviceTypeFilter(e.target.value);
                    setBrandPage(1);
                  }}
                >
                  <option value="">Tất cả loại thiết bị</option>
                  {DEVICE_TYPE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      Có model: {opt.label}
                    </option>
                  ))}
                </select>

                <select
                  value={brandStatusFilter}
                  onChange={(e) => {
                    setBrandStatusFilter(e.target.value as 'all' | 'active' | 'inactive');
                    setBrandPage(1);
                  }}
                >
                  <option value="all">Tất cả trạng thái</option>
                  <option value="active">Đang hoạt động</option>
                  <option value="inactive">Ngưng hoạt động</option>
                </select>
              </div>
            </div>

            {loadingBrands ? (
              <Loading message="Đang tải danh sách hãng sản xuất..." />
            ) : brands.length === 0 ? (
              <EmptyState message="Không tìm thấy hãng sản xuất phù hợp." />
            ) : (
              <>
                <div className={styles.tableWrap}>
                  <table>
                    <thead>
                      <tr>
                        <th>Hãng sản xuất</th>
                        <th>Mã Code</th>
                        <th>Loại thiết bị hỗ trợ</th>
                        <th>Số lượng Model</th>
                        <th>Trạng thái</th>
                        <th>Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {brands.map((brand) => (
                        <tr key={brand.id}>
                          <td>
                            <div className={styles.brandCell}>
                              {brand.logoUrl && (
                                <img
                                  src={brand.logoUrl}
                                  alt={brand.name}
                                  className={styles.brandLogo}
                                  onError={(e) => {
                                    e.currentTarget.style.display = 'none';
                                  }}
                                />
                              )}
                              <div>
                                <strong className={styles.brandName}>{brand.name}</strong>
                                {brand.description && (
                                  <span className={styles.brandDesc}>{brand.description}</span>
                                )}
                              </div>
                            </div>
                          </td>
                          <td>
                            <span className={styles.codePill}>{brand.code}</span>
                          </td>
                          <td>
                            {brand.deviceTypes.length === 0 ? (
                              <span className={styles.brandDesc}>Chưa gắn model</span>
                            ) : (
                              <div className={styles.tagList}>
                                {brand.deviceTypes.map((dt) => (
                                  <span key={dt} className={styles.deviceTag}>
                                    {DEVICE_TYPE_LABELS[dt] ?? dt}
                                  </span>
                                ))}
                              </div>
                            )}
                          </td>
                          <td>
                            <strong>{brand.modelCount}</strong> model
                          </td>
                          <td>
                            <Badge variant={brand.isActive ? 'success' : 'default'}>
                              {brand.isActive ? 'Đang hoạt động' : 'Ngưng hoạt động'}
                            </Badge>
                          </td>
                          <td>
                            <div className={styles.rowActions}>
                              <Button
                                type="button"
                                size="sm"
                                variant="secondary"
                                onClick={() => {
                                  setEditingBrand(brand);
                                  setBrandForm({
                                    name: brand.name,
                                    code: brand.code,
                                    logoUrl: brand.logoUrl ?? '',
                                    description: brand.description ?? '',
                                    isActive: brand.isActive,
                                  });
                                  window.scrollTo({ top: 0, behavior: 'smooth' });
                                }}
                              >
                                Sửa
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant="ghost"
                                onClick={() => handleToggleBrand(brand)}
                              >
                                {brand.isActive ? 'Tắt' : 'Bật'}
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant="danger"
                                onClick={() => setDeletingBrand(brand)}
                              >
                                Xóa
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {brandsTotal > PAGE_SIZE && (
                  <div className={styles.paginationWrap}>
                    <Pagination
                      page={brandPage}
                      pageSize={PAGE_SIZE}
                      total={brandsTotal}
                      onPageChange={setBrandPage}
                    />
                  </div>
                )}
              </>
            )}
          </section>
        </>
      ) : (
        <>
          <form className={styles.editor} onSubmit={handleModelSubmit}>
            <div className={styles.sectionHeading}>
              <div>
                <span className={styles.sectionKicker}>
                  {editingModel ? 'CHỈNH SỬA DÒNG MÁY' : 'TẠO DÒNG MÁY (DEVICE MODEL) MỚI'}
                </span>
                <h2>
                  {editingModel
                    ? `Cập nhật: ${editingModel.brandName} ${editingModel.modelName}`
                    : 'Phân loại theo Loại thiết bị → Hãng sản xuất → Dòng máy'}
                </h2>
              </div>
              {editingModel && (
                <Button type="button" variant="ghost" size="sm" onClick={resetModelForm}>
                  Hủy chỉnh sửa
                </Button>
              )}
            </div>

            {modelError && <ErrorMessage message={modelError} />}

            <div className={styles.formGrid3}>
              <label className={styles.field}>
                <span>
                  1. Loại thiết bị (DeviceType) <em>*</em>
                </span>
                <select
                  value={modelForm.deviceType}
                  onChange={(e) =>
                    setModelForm((prev) => ({
                      ...prev,
                      deviceType: e.target.value as DeviceType,
                    }))
                  }
                >
                  {DEVICE_TYPE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className={styles.field}>
                <span>
                  2. Hãng sản xuất (Brand) <em>*</em>
                </span>
                <select
                  value={modelForm.brandId}
                  onChange={(e) => setModelForm((prev) => ({ ...prev, brandId: e.target.value }))}
                  required
                >
                  <option value="">-- Chọn hãng sản xuất --</option>
                  {allActiveBrands.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name} ({b.code})
                    </option>
                  ))}
                </select>
              </label>

              <label className={styles.field}>
                <span>
                  3. Tên dòng máy (ModelName) <em>*</em>
                </span>
                <input
                  value={modelForm.modelName}
                  onChange={(e) =>
                    setModelForm((prev) => ({ ...prev, modelName: e.target.value }))
                  }
                  placeholder="Ví dụ: iPhone 15 Pro Max, Dell XPS 13 9315..."
                  maxLength={150}
                  required
                />
              </label>
            </div>

            <div className={styles.formGrid2}>
              <label className={styles.field}>
                <span>Mã định danh kỹ thuật (ModelNumber - Tùy chọn)</span>
                <input
                  value={modelForm.modelNumber}
                  onChange={(e) =>
                    setModelForm((prev) => ({ ...prev, modelNumber: e.target.value }))
                  }
                  placeholder="Ví dụ: A3106, SM-S918B, UX3405MA..."
                  maxLength={100}
                />
              </label>

              <label className={styles.field}>
                <span>Năm phát hành (ReleaseYear - Tùy chọn)</span>
                <input
                  type="number"
                  min={1990}
                  max={2100}
                  value={modelForm.releaseYear}
                  onChange={(e) =>
                    setModelForm((prev) => ({ ...prev, releaseYear: e.target.value }))
                  }
                  placeholder="Ví dụ: 2023, 2024..."
                />
              </label>
            </div>

            <div className={styles.formActions}>
              <Button type="submit" isLoading={savingModel}>
                {editingModel ? 'Lưu thay đổi' : 'Thêm dòng máy'}
              </Button>
              <Button type="button" variant="secondary" onClick={resetModelForm}>
                Làm mới
              </Button>
            </div>
          </form>

          <section className={styles.catalog}>
            <div className={styles.catalogHeader}>
              <div>
                <span className={styles.sectionKicker}>DANH MỤC DÒNG MÁY</span>
                <h2>
                  Danh sách DeviceModel <span>({modelsTotal})</span>
                </h2>
              </div>

              <div className={styles.filters}>
                <label className={styles.search}>
                  <span>⌕</span>
                  <input
                    value={modelSearch}
                    onChange={(e) => {
                      setModelSearch(e.target.value);
                      setModelPage(1);
                    }}
                    placeholder="Tìm tên model hoặc mã kỹ thuật..."
                  />
                </label>

                <select
                  aria-label="Lọc loại thiết bị"
                  value={modelDeviceTypeFilter}
                  onChange={(e) => {
                    setModelDeviceTypeFilter(e.target.value);
                    // Reset Brand filter when DeviceType changes so invalid Brand is cleared
                    setModelBrandFilter('');
                    setModelPage(1);
                  }}
                >
                  <option value="">1. Tất cả loại thiết bị</option>
                  {DEVICE_TYPE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>

                <select
                  aria-label="Lọc hãng sản xuất"
                  value={modelBrandFilter}
                  onChange={(e) => {
                    setModelBrandFilter(e.target.value);
                    setModelPage(1);
                  }}
                >
                  <option value="">2. Tất cả hãng tương ứng</option>
                  {filterBrandOptions.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name}
                    </option>
                  ))}
                </select>

                <select
                  value={modelStatusFilter}
                  onChange={(e) => {
                    setModelStatusFilter(e.target.value as 'all' | 'active' | 'inactive');
                    setModelPage(1);
                  }}
                >
                  <option value="all">Tất cả trạng thái</option>
                  <option value="active">Đang hoạt động</option>
                  <option value="inactive">Ngưng hoạt động</option>
                </select>
              </div>
            </div>

            {loadingModels ? (
              <Loading message="Đang tải danh sách dòng máy..." />
            ) : models.length === 0 ? (
              <EmptyState message="Không tìm thấy dòng máy phù hợp với bộ lọc." />
            ) : (
              <>
                <div className={styles.tableWrap}>
                  <table>
                    <thead>
                      <tr>
                        <th>Loại thiết bị</th>
                        <th>Hãng sản xuất</th>
                        <th>Tên dòng máy (Model)</th>
                        <th>Mã kỹ thuật</th>
                        <th>Năm phát hành</th>
                        <th>Trạng thái</th>
                        <th>Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {models.map((m) => (
                        <tr key={m.id}>
                          <td>
                            <span className={styles.deviceTag}>
                              {DEVICE_TYPE_LABELS[m.deviceType] ?? m.deviceType}
                            </span>
                          </td>
                          <td>
                            <strong>{m.brandName}</strong>
                          </td>
                          <td>
                            <strong className={styles.brandName}>{m.modelName}</strong>
                          </td>
                          <td>
                            {m.modelNumber ? (
                              <span className={styles.codePill}>{m.modelNumber}</span>
                            ) : (
                              <span className={styles.brandDesc}>—</span>
                            )}
                          </td>
                          <td>{m.releaseYear ?? '—'}</td>
                          <td>
                            <Badge variant={m.isActive ? 'success' : 'default'}>
                              {m.isActive ? 'Đang hoạt động' : 'Ngưng hoạt động'}
                            </Badge>
                          </td>
                          <td>
                            <div className={styles.rowActions}>
                              <Button
                                type="button"
                                size="sm"
                                variant="secondary"
                                onClick={() => {
                                  setEditingModel(m);
                                  setModelForm({
                                    deviceType: m.deviceType,
                                    brandId: m.brandId,
                                    modelName: m.modelName,
                                    modelNumber: m.modelNumber ?? '',
                                    releaseYear: m.releaseYear ? String(m.releaseYear) : '',
                                    isActive: m.isActive,
                                  });
                                  window.scrollTo({ top: 0, behavior: 'smooth' });
                                }}
                              >
                                Sửa
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant="ghost"
                                onClick={() => handleToggleModel(m)}
                              >
                                {m.isActive ? 'Tắt' : 'Bật'}
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant="danger"
                                onClick={() => setDeletingModel(m)}
                              >
                                Xóa
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {modelsTotal > PAGE_SIZE && (
                  <div className={styles.paginationWrap}>
                    <Pagination
                      page={modelPage}
                      pageSize={PAGE_SIZE}
                      total={modelsTotal}
                      onPageChange={setModelPage}
                    />
                  </div>
                )}
              </>
            )}
          </section>
        </>
      )}

      <ConfirmDialog
        isOpen={Boolean(deletingBrand)}
        onCancel={() => setDeletingBrand(null)}
        onConfirm={handleDeleteBrand}
        title="Xóa hãng sản xuất"
        message={`Bạn có chắc muốn xóa hãng "${deletingBrand?.name ?? ''}"? Hãng đang có dòng máy hoặc thiết bị tham chiếu sẽ không thể xóa.`}
        confirmLabel="Xóa hãng"
        isDangerous
        isLoading={isDeletingBrand}
      />

      <ConfirmDialog
        isOpen={Boolean(deletingModel)}
        onCancel={() => setDeletingModel(null)}
        onConfirm={handleDeleteModel}
        title="Xóa dòng máy"
        message={`Bạn có chắc muốn xóa dòng máy "${deletingModel?.brandName ?? ''} ${deletingModel?.modelName ?? ''}"?`}
        confirmLabel="Xóa dòng máy"
        isDangerous
        isLoading={isDeletingModel}
      />
    </div>
  );
}
