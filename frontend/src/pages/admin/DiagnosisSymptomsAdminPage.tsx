import { useEffect, useMemo, useState } from 'react';
import { Badge, Button, EmptyState, ErrorMessage, Loading, Pagination } from '../../components/common';
import { diagnosisService } from '../../services/diagnosisService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { DiagnosisSymptom, DeviceType } from '../../types/diagnosis.types';
import styles from './DiagnosisSymptomsAdminPage.module.css';

const deviceTypes: DeviceType[] = ['Phone', 'Laptop', 'Electronics', 'Tablet', 'Smartwatch'];
const pageSize = 10;

const deviceLabels: Record<DeviceType, string> = {
  Phone: 'Điện thoại',
  Laptop: 'Laptop',
  Electronics: 'Điện tử',
  Tablet: 'Máy tính bảng',
  Smartwatch: 'Đồng hồ thông minh',
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

  useEffect(() => { void load(); }, []);

  const filteredItems = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi-VN');
    return items
      .filter((item) => {
        const matchesSearch = !query || item.name.toLocaleLowerCase('vi-VN').includes(query);
        const matchesDevice = deviceFilter === 'all' || item.deviceType === deviceFilter;
        const matchesStatus = statusFilter === 'all'
          || (statusFilter === 'active' ? item.isActive : !item.isActive);
        return matchesSearch && matchesDevice && matchesStatus;
      })
      .sort((a, b) => a.deviceType.localeCompare(b.deviceType) || a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  }, [items, search, deviceFilter, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredItems.length / pageSize));
  const visibleItems = filteredItems.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => {
    setPage((currentPage) => Math.min(currentPage, totalPages));
  }, [totalPages]);

  const reset = () => {
    setEditingId(null);
    setName('');
    setDeviceType('Phone');
    setSortOrder(1);
  };

  const save = async () => {
    setErrorMessage(null);
    if (!name.trim()) {
      setErrorMessage('Vui lòng nhập tên tiêu chí.');
      return;
    }
    if (sortOrder < 0) {
      setErrorMessage('Thứ tự không được nhỏ hơn 0.');
      return;
    }
    try {
      const payload = { name: name.trim(), deviceType, sortOrder };
      if (editingId) await diagnosisService.update(editingId, payload);
      else await diagnosisService.create(payload);
      showSuccess(editingId ? 'Đã cập nhật tiêu chí.' : 'Đã thêm tiêu chí.');
      reset();
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
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
      showSuccess(item.isActive ? 'Đã tắt tiêu chí.' : 'Đã bật tiêu chí.');
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const activeCount = items.filter((item) => item.isActive).length;

  return (
    <main className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>CẤU HÌNH NGHIỆP VỤ</span>
          <h1 className={styles.title}>Checklist chẩn đoán</h1>
          <p className={styles.subtitle}>Quản lý các tiêu chí kỹ thuật viên sử dụng khi kiểm tra thiết bị.</p>
        </div>
        <div className={styles.summary}><strong>{activeCount}</strong><span>tiêu chí đang dùng</span></div>
      </header>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <section className={styles.form} aria-labelledby="diagnosis-editor">
        <div className={styles.sectionHeading}>
          <div><span className={styles.sectionKicker}>{editingId ? 'CHỈNH SỬA' : 'TẠO MỚI'}</span><h2 id="diagnosis-editor">{editingId ? 'Cập nhật tiêu chí' : 'Thêm tiêu chí chẩn đoán'}</h2></div>
          {editingId && <Button variant="ghost" size="sm" onClick={reset}>Hủy chỉnh sửa</Button>}
        </div>
        <div className={styles.formGrid}>
          <label className={styles.field}><span>Tên tiêu chí <em>*</em></span><input maxLength={200} value={name} placeholder="VD: Kiểm tra camera sau" onChange={(e) => setName(e.target.value)} /></label>
          <label className={styles.field}><span>Loại thiết bị <em>*</em></span><select value={deviceType} onChange={(e) => setDeviceType(e.target.value as DeviceType)}>{deviceTypes.map((type) => <option key={type} value={type}>{deviceLabels[type]}</option>)}</select></label>
          <label className={styles.field}><span>Thứ tự</span><input type="number" min={0} value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value))} /></label>
        </div>
        <div className={styles.actions}><Button onClick={save}>{editingId ? 'Lưu thay đổi' : 'Thêm tiêu chí'}</Button>{!editingId && <Button variant="secondary" onClick={reset}>Làm mới</Button>}</div>
      </section>

      <section className={styles.catalog} aria-labelledby="diagnosis-list">
        <div className={styles.catalogHeader}>
          <div><span className={styles.sectionKicker}>DANH MỤC</span><h2 id="diagnosis-list">Danh sách tiêu chí <span>{items.length}</span></h2></div>
          <div className={styles.filters}>
            <label className={styles.search}><span aria-hidden="true">⌕</span><input value={search} placeholder="Tìm tiêu chí..." aria-label="Tìm tiêu chí" onChange={(e) => { setSearch(e.target.value); setPage(1); }} /></label>
            <select value={deviceFilter} aria-label="Lọc loại thiết bị" onChange={(e) => { setDeviceFilter(e.target.value as typeof deviceFilter); setPage(1); }}><option value="all">Tất cả thiết bị</option>{deviceTypes.map((type) => <option key={type} value={type}>{deviceLabels[type]}</option>)}</select>
            <select value={statusFilter} aria-label="Lọc trạng thái" onChange={(e) => { setStatusFilter(e.target.value as typeof statusFilter); setPage(1); }}><option value="all">Tất cả trạng thái</option><option value="active">Đang dùng</option><option value="inactive">Đã tắt</option></select>
          </div>
        </div>

        {isLoading ? <Loading /> : visibleItems.length === 0 ? (
          <EmptyState message={items.length === 0 ? 'Chưa có tiêu chí chẩn đoán nào.' : 'Không tìm thấy tiêu chí phù hợp.'} action={items.length > 0 ? <Button variant="ghost" size="sm" onClick={() => { setSearch(''); setDeviceFilter('all'); setStatusFilter('all'); setPage(1); }}>Xóa bộ lọc</Button> : undefined} />
        ) : (
          <>
            <div className={styles.tableWrap}><table><thead><tr><th>Tiêu chí</th><th>Thiết bị</th><th>Thứ tự</th><th>Trạng thái</th><th aria-label="Thao tác" /></tr></thead><tbody>{visibleItems.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td><td><span className={styles.deviceTag}>{deviceLabels[item.deviceType]}</span></td><td><span className={styles.order}>{item.sortOrder}</span></td><td><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Đang dùng' : 'Đã tắt'}</Badge></td><td><div className={styles.rowActions}><Button size="sm" variant="secondary" onClick={() => edit(item)}>Sửa</Button><Button size="sm" variant={item.isActive ? 'danger' : 'primary'} onClick={() => toggle(item)}>{item.isActive ? 'Tắt' : 'Bật'}</Button></div></td></tr>)}</tbody></table></div>
            <Pagination page={page} pageSize={pageSize} total={filteredItems.length} onPageChange={setPage} />
          </>
        )}
      </section>
    </main>
  );
}
