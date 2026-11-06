import { useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  EmptyState,
  ErrorMessage,
  Loading,
  Pagination,
} from '../../components/common';
import { diagnosisService } from '../../services/diagnosisService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { DiagnosisSymptom, DeviceType } from '../../types/diagnosis.types';
import styles from './DiagnosisSymptomsAdminPage.module.css';

const deviceTypes: DeviceType[] = ['Phone', 'Laptop', 'Tablet', 'Smartwatch', 'Electronics'];
const pageSize = 10;

const deviceLabels: Record<DeviceType, string> = {
  Phone: 'Điện thoại',
  Laptop: 'Laptop',
  Tablet: 'Máy tính bảng',
  Smartwatch: 'Đồng hồ thông minh',
  Electronics: 'Thiết bị điện tử',
};

const deviceIcons: Record<DeviceType, string> = {
  Phone: '📱',
  Laptop: '💻',
  Tablet: '📟',
  Smartwatch: '⌚',
  Electronics: '🔌',
};

export default function DiagnosisSymptomsAdminPage() {
  const { showSuccess } = useToast();
  const [items, setItems] = useState<DiagnosisSymptom[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [deviceType, setDeviceType] = useState<DeviceType>('Phone');
  const [sortOrder, setSortOrder] = useState(1);
  const [search, setSearch] = useState('');
  const [deviceFilter, setDeviceFilter] = useState<'all' | DeviceType>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      setItems(await diagnosisService.listAdmin());
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const filteredItems = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi-VN');
    return items
      .filter((item) => {
        const matchesSearch = !query || item.name.toLocaleLowerCase('vi-VN').includes(query);
        const matchesDevice = deviceFilter === 'all' || item.deviceType === deviceFilter;
        const matchesStatus =
          statusFilter === 'all' ||
          (statusFilter === 'active' ? item.isActive : !item.isActive);
        return matchesSearch && matchesDevice && matchesStatus;
      })
      .sort(
        (a, b) =>
          a.deviceType.localeCompare(b.deviceType) ||
          a.sortOrder - b.sortOrder ||
          a.name.localeCompare(b.name)
      );
  }, [items, search, deviceFilter, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredItems.length / pageSize));
  const visibleItems = filteredItems.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => {
    setPage((currentPage) => Math.min(currentPage, totalPages));
  }, [totalPages]);

  const reset = () => {
    setEditingId(null);
    setName('');
    setDeviceType(deviceFilter !== 'all' ? deviceFilter : 'Phone');
    setSortOrder(1);
  };

  const save = async () => {
    setErrorMessage(null);
    if (!name.trim()) {
      setErrorMessage('Vui lòng nhập tên triệu chứng / tiêu chí chẩn đoán.');
      return;
    }
    if (sortOrder < 0) {
      setErrorMessage('Thứ tự không được nhỏ hơn 0.');
      return;
    }
    setIsSaving(true);
    try {
      const payload = { name: name.trim(), deviceType, sortOrder };
      if (editingId) await diagnosisService.update(editingId, payload);
      else await diagnosisService.create(payload);
      showSuccess(editingId ? 'Đã cập nhật triệu chứng chẩn đoán.' : 'Đã thêm triệu chứng chẩn đoán.');
      reset();
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsSaving(false);
    }
  };

  const edit = (item: DiagnosisSymptom) => {
    setEditingId(item.id);
    setName(item.name);
    setDeviceType(item.deviceType);
    setSortOrder(item.sortOrder);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const toggle = async (item: DiagnosisSymptom) => {
    try {
      await diagnosisService.toggleActive(item.id, !item.isActive);
      showSuccess(item.isActive ? 'Đã tạm tắt tiêu chí.' : 'Đã kích hoạt tiêu chí.');
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const activeCount = items.filter((item) => item.isActive).length;
  const inactiveCount = items.length - activeCount;

  const handleSelectDeviceTab = (target: 'all' | DeviceType) => {
    setDeviceFilter(target);
    setPage(1);
    if (!editingId && target !== 'all') {
      setDeviceType(target);
    }
  };

  return (
    <main className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>CHUẨN HÓA KỸ THUẬT — FIXORA</span>
          <h1 className={styles.title}>Triệu chứng &amp; Checklist Chẩn đoán</h1>
          <p className={styles.subtitle}>
            Quản lý thư viện triệu chứng lỗi và checklist kiểm tra ban đầu giúp kỹ thuật viên chẩn đoán nhanh theo từng dòng thiết bị.
          </p>
        </div>
      </header>

      {/* 3 Thẻ KPI tổng quan */}
      <div className={styles.kpiRow}>
        <button
          type="button"
          className={`${styles.kpiCard} ${statusFilter === 'all' ? styles.kpiCardActive : ''}`}
          onClick={() => {
            setStatusFilter('all');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>🩺</span>
          <div>
            <strong>{items.length}</strong>
            <span>Tổng triệu chứng / tiêu chí</span>
          </div>
        </button>
        <button
          type="button"
          className={`${styles.kpiCard} ${
            statusFilter === 'active' ? styles.kpiCardActive : ''
          }`}
          onClick={() => {
            setStatusFilter('active');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>✅</span>
          <div>
            <strong>{activeCount}</strong>
            <span>Đang sử dụng</span>
          </div>
        </button>
        <button
          type="button"
          className={`${styles.kpiCard} ${
            statusFilter === 'inactive' ? styles.kpiCardActive : ''
          }`}
          onClick={() => {
            setStatusFilter('inactive');
            setPage(1);
          }}
        >
          <span className={styles.kpiIcon}>⏸️</span>
          <div>
            <strong>{inactiveCount}</strong>
            <span>Đã tạm tắt</span>
          </div>
        </button>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <section className={styles.form} aria-labelledby="diagnosis-editor">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.sectionKicker}>
              {editingId ? 'CHỈNH SỬA TIÊU CHÍ' : 'THÊM MỚI TIÊU CHÍ'}
            </span>
            <h2 id="diagnosis-editor">
              {editingId ? 'Cập nhật triệu chứng chẩn đoán' : 'Thêm triệu chứng / tiêu chí kiểm tra'}
            </h2>
          </div>
          {editingId && (
            <Button variant="ghost" size="sm" onClick={reset}>
              ✕ Hủy chỉnh sửa
            </Button>
          )}
        </div>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>
              Tên triệu chứng / Hạng mục kiểm tra <em>*</em>
            </span>
            <input
              maxLength={200}
              value={name}
              placeholder="VD: Màn hình sọc xanh / liệt cảm ứng cục bộ"
              onChange={(e) => setName(e.target.value)}
            />
          </label>
          <label className={styles.field}>
            <span>
              Loại thiết bị áp dụng <em>*</em>
            </span>
            <select
              value={deviceType}
              onChange={(e) => setDeviceType(e.target.value as DeviceType)}
            >
              {deviceTypes.map((type) => (
                <option key={type} value={type}>
                  {deviceIcons[type]} {deviceLabels[type]}
                </option>
              ))}
            </select>
          </label>
          <label className={styles.field}>
            <span>Thứ tự ưu tiên</span>
            <input
              type="number"
              min={0}
              value={sortOrder}
              onChange={(e) => setSortOrder(Number(e.target.value))}
            />
          </label>
        </div>
        <div className={styles.actions}>
          <Button onClick={() => void save()} isLoading={isSaving}>
            {editingId ? 'Lưu thay đổi' : '+ Thêm triệu chứng'}
          </Button>
          {!editingId && (
            <Button variant="secondary" onClick={reset}>
              Làm mới
            </Button>
          )}
        </div>
      </section>

      {/* Thanh pill chọn nhanh theo loại thiết bị */}
      <div className={styles.deviceTabsBar}>
        <button
          type="button"
          className={`${styles.deviceTab} ${
            deviceFilter === 'all' ? styles.deviceTabActive : ''
          }`}
          onClick={() => handleSelectDeviceTab('all')}
        >
          <span>Tất cả thiết bị</span>
          <strong>{items.length}</strong>
        </button>
        {deviceTypes.map((type) => {
          const count = items.filter((i) => i.deviceType === type).length;
          return (
            <button
              key={type}
              type="button"
              className={`${styles.deviceTab} ${
                deviceFilter === type ? styles.deviceTabActive : ''
              }`}
              onClick={() => handleSelectDeviceTab(deviceFilter === type ? 'all' : type)}
            >
              <span>
                {deviceIcons[type]} {deviceLabels[type]}
              </span>
              <strong>{count}</strong>
            </button>
          );
        })}
      </div>

      <section className={styles.catalog} aria-labelledby="diagnosis-list">
        <div className={styles.catalogHeader}>
          <div>
            <span className={styles.sectionKicker}>DANH MỤC TRIỆU CHỨNG</span>
            <h2 id="diagnosis-list">
              Danh sách tiêu chí chẩn đoán <span>({filteredItems.length})</span>
            </h2>
          </div>
          <div className={styles.filters}>
            <label className={styles.search}>
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                placeholder="Tìm nhanh theo tên triệu chứng..."
                aria-label="Tìm tiêu chí"
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
              />
            </label>
            <select
              value={statusFilter}
              aria-label="Lọc trạng thái"
              onChange={(e) => {
                setStatusFilter(e.target.value as typeof statusFilter);
                setPage(1);
              }}
            >
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang sử dụng</option>
              <option value="inactive">Đã tạm tắt</option>
            </select>
          </div>
        </div>

        {isLoading ? (
          <Loading />
        ) : visibleItems.length === 0 ? (
          <EmptyState
            message={
              items.length === 0
                ? 'Chưa có tiêu chí chẩn đoán nào.'
                : 'Không tìm thấy tiêu chí phù hợp với bộ lọc.'
            }
            action={
              items.length > 0 ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setSearch('');
                    setDeviceFilter('all');
                    setStatusFilter('all');
                    setPage(1);
                  }}
                >
                  Xóa bộ lọc
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            <div className={styles.tableWrap}>
              <table>
                <thead>
                  <tr>
                    <th>Thứ tự</th>
                    <th>Triệu chứng / Tiêu chí kiểm tra</th>
                    <th>Loại thiết bị</th>
                    <th>Trạng thái</th>
                    <th className={styles.rightCol}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {visibleItems.map((item) => (
                    <tr key={item.id}>
                      <td className={styles.orderCell}>
                        <span className={styles.orderBadge}>#{item.sortOrder}</span>
                      </td>
                      <td>
                        <strong className={styles.symptomName}>{item.name}</strong>
                      </td>
                      <td>
                        <span className={styles.deviceTag}>
                          {deviceIcons[item.deviceType]} {deviceLabels[item.deviceType]}
                        </span>
                      </td>
                      <td>
                        <Badge variant={item.isActive ? 'success' : 'default'}>
                          {item.isActive ? 'Đang dùng' : 'Đã tắt'}
                        </Badge>
                      </td>
                      <td>
                        <div className={styles.rowActions}>
                          <Button size="sm" variant="secondary" onClick={() => edit(item)}>
                            Sửa
                          </Button>
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => void toggle(item)}
                          >
                            {item.isActive ? 'Tạm tắt' : 'Kích hoạt'}
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
                pageSize={pageSize}
                total={filteredItems.length}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </section>
    </main>
  );
}
