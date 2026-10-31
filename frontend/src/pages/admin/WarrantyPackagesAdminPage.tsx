import { useEffect, useMemo, useState } from 'react';
import { Badge, Button, EmptyState, ErrorMessage, Loading } from '../../components/common';
import { warrantyPackageService } from '../../services/warrantyPackageService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { WarrantyPackage } from '../../types/warrantyPackage.types';
import styles from './WarrantyPackagesAdminPage.module.css';

type PackageForm = {
  name: string;
  description: string;
  durationMonths: number;
  terms: string;
};

const emptyForm: PackageForm = { name: '', description: '', durationMonths: 1, terms: '' };

export default function WarrantyPackagesAdminPage() {
  const { showSuccess } = useToast();
  const [items, setItems] = useState<WarrantyPackage[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<PackageForm>(emptyForm);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const load = async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      setItems(await warrantyPackageService.listAdmin());
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const filteredItems = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi-VN');
    return items.filter((item) => {
      const matchesStatus = statusFilter === 'all'
        || (statusFilter === 'active' ? item.isActive : !item.isActive);
      const matchesSearch = !query
        || `${item.name} ${item.description ?? ''} ${item.terms}`.toLocaleLowerCase('vi-VN').includes(query);
      return matchesStatus && matchesSearch;
    });
  }, [items, search, statusFilter]);

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm);
  };

  const save = async () => {
    setErrorMessage(null);
    if (!form.name.trim()) {
      setErrorMessage('Vui lòng nhập tên gói bảo hành.');
      return;
    }
    if (form.durationMonths <= 0) {
      setErrorMessage('Thời hạn phải lớn hơn 0 tháng.');
      return;
    }
    if (!form.terms.trim()) {
      setErrorMessage('Vui lòng nhập điều khoản bảo hành.');
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        durationMonths: form.durationMonths,
        terms: form.terms.trim(),
      };
      if (editingId) {
        await warrantyPackageService.update(editingId, payload);
        showSuccess('Đã cập nhật gói bảo hành.');
      } else {
        await warrantyPackageService.create(payload);
        showSuccess('Đã tạo gói bảo hành.');
      }
      resetForm();
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsSaving(false);
    }
  };

  const edit = (item: WarrantyPackage) => {
    setEditingId(item.id);
    setForm({
      name: item.name,
      description: item.description ?? '',
      durationMonths: item.durationMonths,
      terms: item.terms,
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const toggle = async (item: WarrantyPackage) => {
    setErrorMessage(null);
    try {
      await warrantyPackageService.toggle(item.id, !item.isActive);
      showSuccess(item.isActive ? 'Đã ngừng hoạt động gói bảo hành.' : 'Đã kích hoạt gói bảo hành.');
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    }
  };

  const remove = async (item: WarrantyPackage) => {
    if (!window.confirm(`Xóa mềm gói "${item.name}"?`)) return;
    setErrorMessage(null);
    try {
      await warrantyPackageService.remove(item.id);
      showSuccess('Đã xóa gói bảo hành.');
      if (editingId === item.id) resetForm();
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    }
  };

  const activeCount = items.filter((item) => item.isActive).length;

  return (
    <main className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>CẤU HÌNH NGHIỆP VỤ</span>
          <h1 className={styles.title}>Gói bảo hành</h1>
          <p className={styles.subtitle}>Chuẩn hóa thời hạn và điều khoản khi cấp bảo hành cho khách hàng.</p>
        </div>
        <div className={styles.headerSummary}>
          <strong>{activeCount}</strong>
          <span>gói đang hoạt động</span>
        </div>
      </header>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <section className={styles.editor} aria-labelledby="warranty-package-editor">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.sectionKicker}>{editingId ? 'CHỈNH SỬA' : 'TẠO MỚI'}</span>
            <h2 id="warranty-package-editor">{editingId ? 'Cập nhật gói bảo hành' : 'Thêm gói bảo hành'}</h2>
          </div>
          {editingId && <Button variant="ghost" size="sm" onClick={resetForm}>Hủy chỉnh sửa</Button>}
        </div>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>Tên gói <em>*</em></span>
            <input value={form.name} maxLength={150} placeholder="VD: Bảo hành linh kiện 6 tháng" onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </label>
          <label className={styles.field}>
            <span>Thời hạn <em>*</em></span>
            <div className={styles.numberInput}>
              <input type="number" min={1} max={120} value={form.durationMonths} onChange={(e) => setForm({ ...form, durationMonths: Number(e.target.value) })} />
              <span>tháng</span>
            </div>
          </label>
          <label className={`${styles.field} ${styles.fullField}`}>
            <span>Mô tả</span>
            <input value={form.description} maxLength={500} placeholder="Mô tả ngắn để lễ tân dễ lựa chọn" onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </label>
          <label className={`${styles.field} ${styles.fullField}`}>
            <span>Điều khoản <em>*</em></span>
            <textarea rows={3} maxLength={500} value={form.terms} placeholder="Nêu rõ phạm vi và điều kiện bảo hành" onChange={(e) => setForm({ ...form, terms: e.target.value })} />
            <small>{form.terms.length}/500 ký tự</small>
          </label>
        </div>
        <div className={styles.formActions}>
          <Button onClick={save} isLoading={isSaving}>{editingId ? 'Lưu thay đổi' : 'Tạo gói bảo hành'}</Button>
          {!editingId && <Button variant="secondary" onClick={resetForm}>Làm mới</Button>}
        </div>
      </section>

      <section className={styles.catalog} aria-labelledby="warranty-package-list">
        <div className={styles.catalogHeader}>
          <div>
            <span className={styles.sectionKicker}>DANH MỤC</span>
            <h2 id="warranty-package-list">Danh sách gói bảo hành <span>{items.length}</span></h2>
          </div>
          <div className={styles.filters}>
            <label className={styles.search}>
              <span aria-hidden="true">⌕</span>
              <input value={search} placeholder="Tìm theo tên, điều khoản..." aria-label="Tìm gói bảo hành" onChange={(e) => setSearch(e.target.value)} />
            </label>
            <select value={statusFilter} aria-label="Lọc trạng thái" onChange={(e) => setStatusFilter(e.target.value as typeof statusFilter)}>
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang hoạt động</option>
              <option value="inactive">Đã ngừng</option>
            </select>
          </div>
        </div>

        {isLoading ? <Loading /> : filteredItems.length === 0 ? (
          <EmptyState message={items.length === 0 ? 'Chưa có gói bảo hành nào.' : 'Không tìm thấy gói phù hợp.'} action={items.length > 0 ? <Button variant="ghost" size="sm" onClick={() => { setSearch(''); setStatusFilter('all'); }}>Xóa bộ lọc</Button> : undefined} />
        ) : (
          <div className={styles.tableWrap}>
            <table>
              <thead><tr><th>Gói bảo hành</th><th>Thời hạn</th><th>Điều khoản snapshot</th><th>Trạng thái</th><th aria-label="Thao tác" /></tr></thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong className={styles.packageName}>{item.name}</strong>{item.description && <span className={styles.description}>{item.description}</span>}</td>
                    <td><span className={styles.duration}>{item.durationMonths}</span> tháng</td>
                    <td><span className={styles.terms}>{item.terms}</span></td>
                    <td><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Đang hoạt động' : 'Đã ngừng'}</Badge></td>
                    <td><div className={styles.rowActions}><Button size="sm" variant="secondary" onClick={() => edit(item)}>Sửa</Button><Button size="sm" variant={item.isActive ? 'danger' : 'primary'} onClick={() => toggle(item)}>{item.isActive ? 'Tắt' : 'Bật'}</Button><Button size="sm" variant="ghost" onClick={() => remove(item)} aria-label={`Xóa ${item.name}`}>Xóa</Button></div></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
