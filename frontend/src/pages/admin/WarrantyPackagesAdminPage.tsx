import { useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  ConfirmDialog,
  EmptyState,
  ErrorMessage,
  Loading,
} from '../../components/common';
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

const emptyForm: PackageForm = { name: '', description: '', durationMonths: 6, terms: '' };
const DURATION_PRESETS = [1, 3, 6, 12, 24];

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

  const [deletingTarget, setDeletingTarget] = useState<WarrantyPackage | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

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
      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' ? item.isActive : !item.isActive);
      const matchesSearch =
        !query ||
        `${item.name} ${item.description ?? ''} ${item.terms}`
          .toLocaleLowerCase('vi-VN')
          .includes(query);
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
      showSuccess(
        item.isActive ? 'Đã ngừng hoạt động gói bảo hành.' : 'Đã kích hoạt gói bảo hành.'
      );
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    }
  };

  const confirmRemove = async () => {
    if (!deletingTarget) return;
    setIsDeleting(true);
    setErrorMessage(null);
    try {
      await warrantyPackageService.remove(deletingTarget.id);
      showSuccess('Đã xóa gói bảo hành.');
      if (editingId === deletingTarget.id) resetForm();
      setDeletingTarget(null);
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error).message);
    } finally {
      setIsDeleting(false);
    }
  };

  const activeCount = items.filter((item) => item.isActive).length;
  const inactiveCount = items.length - activeCount;

  return (
    <main className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>CHÍNH SÁCH HẬU MÃI — FIXORA</span>
          <h1 className={styles.title}>Quản lý Gói Bảo hành</h1>
          <p className={styles.subtitle}>
            Chuẩn hóa thời hạn, phạm vi áp dụng và điều khoản snapshot khi cấp chứng nhận bảo hành điện tử cho khách hàng.
          </p>
        </div>
      </header>

      {/* 3 Thẻ KPI lọc nhanh */}
      <div className={styles.kpiRow}>
        <button
          type="button"
          className={`${styles.kpiCard} ${statusFilter === 'all' ? styles.kpiCardActive : ''}`}
          onClick={() => setStatusFilter('all')}
        >
          <span className={styles.kpiIcon}>🛡️</span>
          <div>
            <strong>{items.length}</strong>
            <span>Tổng số gói bảo hành</span>
          </div>
        </button>
        <button
          type="button"
          className={`${styles.kpiCard} ${
            statusFilter === 'active' ? styles.kpiCardActive : ''
          }`}
          onClick={() => setStatusFilter('active')}
        >
          <span className={styles.kpiIcon}>✅</span>
          <div>
            <strong>{activeCount}</strong>
            <span>Đang hoạt động</span>
          </div>
        </button>
        <button
          type="button"
          className={`${styles.kpiCard} ${
            statusFilter === 'inactive' ? styles.kpiCardActive : ''
          }`}
          onClick={() => setStatusFilter('inactive')}
        >
          <span className={styles.kpiIcon}>⏸️</span>
          <div>
            <strong>{inactiveCount}</strong>
            <span>Đã ngừng áp dụng</span>
          </div>
        </button>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <section className={styles.editor} aria-labelledby="warranty-package-editor">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.sectionKicker}>
              {editingId ? 'CHỈNH SỬA GÓI BẢO HÀNH' : 'TẠO GÓI BẢO HÀNH MỚI'}
            </span>
            <h2 id="warranty-package-editor">
              {editingId ? 'Cập nhật thông tin gói bảo hành' : 'Thêm gói bảo hành tiêu chuẩn'}
            </h2>
          </div>
          {editingId && (
            <Button variant="ghost" size="sm" onClick={resetForm}>
              ✕ Hủy chỉnh sửa
            </Button>
          )}
        </div>
        <div className={styles.formGrid}>
          <label className={styles.field}>
            <span>
              Tên gói bảo hành <em>*</em>
            </span>
            <input
              value={form.name}
              maxLength={150}
              placeholder="VD: Bảo hành linh kiện tiêu chuẩn 6 tháng"
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </label>
          <div className={styles.field}>
            <span>
              Thời hạn bảo hành <em>*</em>
            </span>
            <div className={styles.durationControlWrap}>
              <div className={styles.numberInput}>
                <input
                  type="number"
                  min={1}
                  max={120}
                  value={form.durationMonths}
                  onChange={(e) => setForm({ ...form, durationMonths: Number(e.target.value) })}
                />
                <span>tháng</span>
              </div>
              <div className={styles.presetPills}>
                {DURATION_PRESETS.map((m) => (
                  <button
                    key={m}
                    type="button"
                    className={`${styles.presetPill} ${
                      form.durationMonths === m ? styles.presetPillActive : ''
                    }`}
                    onClick={() => setForm({ ...form, durationMonths: m })}
                  >
                    {m}T
                  </button>
                ))}
              </div>
            </div>
          </div>
          <label className={`${styles.field} ${styles.fullField}`}>
            <span>Mô tả ngắn (hiển thị gợi ý cho lễ tân)</span>
            <input
              value={form.description}
              maxLength={500}
              placeholder="VD: Áp dụng cho dịch vụ thay màn hình, thay pin chính hãng..."
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </label>
          <label className={`${styles.field} ${styles.fullField}`}>
            <span>
              Điều khoản bảo hành chi tiết (in trên phiếu bảo hành) <em>*</em>
            </span>
            <textarea
              rows={3}
              maxLength={500}
              value={form.terms}
              placeholder="Nêu rõ phạm vi bảo hành và các trường hợp từ chối bảo hành (rơi vỡ, vào nước, mất tem)..."
              onChange={(e) => setForm({ ...form, terms: e.target.value })}
            />
            <small>{form.terms.length}/500 ký tự</small>
          </label>
        </div>
        <div className={styles.formActions}>
          <Button onClick={() => void save()} isLoading={isSaving}>
            {editingId ? 'Lưu thay đổi' : '+ Tạo gói bảo hành'}
          </Button>
          {!editingId && (
            <Button variant="secondary" onClick={resetForm}>
              Làm mới form
            </Button>
          )}
        </div>
      </section>

      <section className={styles.catalog} aria-labelledby="warranty-package-list">
        <div className={styles.catalogHeader}>
          <div>
            <span className={styles.sectionKicker}>DANH MỤC HIỆN HÀNH</span>
            <h2 id="warranty-package-list">
              Danh sách gói bảo hành <span>({filteredItems.length})</span>
            </h2>
          </div>
          <div className={styles.filters}>
            <label className={styles.search}>
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                placeholder="Tìm theo tên gói, mô tả hoặc điều khoản..."
                aria-label="Tìm gói bảo hành"
                onChange={(e) => setSearch(e.target.value)}
              />
            </label>
            <select
              value={statusFilter}
              aria-label="Lọc trạng thái"
              onChange={(e) => setStatusFilter(e.target.value as typeof statusFilter)}
            >
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang hoạt động</option>
              <option value="inactive">Đã ngừng</option>
            </select>
          </div>
        </div>

        {isLoading ? (
          <Loading />
        ) : filteredItems.length === 0 ? (
          <EmptyState
            message={
              items.length === 0
                ? 'Chưa có gói bảo hành nào.'
                : 'Không tìm thấy gói bảo hành phù hợp.'
            }
            action={
              items.length > 0 ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setSearch('');
                    setStatusFilter('all');
                  }}
                >
                  Xóa bộ lọc
                </Button>
              ) : undefined
            }
          />
        ) : (
          <div className={styles.tableWrap}>
            <table>
              <thead>
                <tr>
                  <th>Tên gói &amp; Mô tả</th>
                  <th>Thời hạn</th>
                  <th>Điều khoản áp dụng</th>
                  <th>Trạng thái</th>
                  <th className={styles.rightCol}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong className={styles.packageName}>{item.name}</strong>
                      {item.description && (
                        <span className={styles.description}>{item.description}</span>
                      )}
                    </td>
                    <td>
                      <span className={styles.durationBadge}>
                        🗓️ {item.durationMonths} tháng
                      </span>
                    </td>
                    <td>
                      <span className={styles.terms}>{item.terms}</span>
                    </td>
                    <td>
                      <Badge variant={item.isActive ? 'success' : 'default'}>
                        {item.isActive ? 'Đang hoạt động' : 'Đã ngừng'}
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
                          {item.isActive ? 'Ngừng' : 'Kích hoạt'}
                        </Button>
                        <Button
                          size="sm"
                          variant="danger"
                          onClick={() => setDeletingTarget(item)}
                          aria-label={`Xóa ${item.name}`}
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
        )}
      </section>

      <ConfirmDialog
        isOpen={!!deletingTarget}
        title="Xác nhận xóa gói bảo hành"
        message={
          deletingTarget
            ? `Bạn có chắc chắn muốn xóa mềm gói bảo hành "${deletingTarget.name}" (${deletingTarget.durationMonths} tháng)? Các phiếu bảo hành đã cấp trước đó vẫn giữ nguyên điều khoản snapshot.`
            : ''
        }
        confirmLabel="Xác nhận xóa"
        cancelLabel="Hủy"
        isDangerous
        isLoading={isDeleting}
        onConfirm={() => void confirmRemove()}
        onCancel={() => setDeletingTarget(null)}
      />
    </main>
  );
}
