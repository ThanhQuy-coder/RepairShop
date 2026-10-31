import { useEffect, useState } from 'react';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { warrantyPackageService } from '../../services/warrantyPackageService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { WarrantyPackage } from '../../types/warrantyPackage.types';

export default function WarrantyPackagesAdminPage() {
  const { showSuccess } = useToast();
  const [items, setItems] = useState<WarrantyPackage[]>([]);
  const [editing, setEditing] = useState<string | null>(null);
  const [form, setForm] = useState({ name: '', description: '', durationMonths: 1, terms: '' });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const load = async () => { try { setItems(await warrantyPackageService.listAdmin()); } catch (e) { setError(extractApiError(e).message); } finally { setLoading(false); } };
  useEffect(() => { load(); }, []);
  const reset = () => { setEditing(null); setForm({ name: '', description: '', durationMonths: 1, terms: '' }); };
  const save = async () => {
    if (!form.name.trim() || form.durationMonths <= 0 || !form.terms.trim()) { setError('Tên, thời hạn và điều khoản là bắt buộc.'); return; }
    try {
      if (editing) await warrantyPackageService.update(editing, form);
      else await warrantyPackageService.create(form);
      showSuccess(editing ? 'Đã cập nhật gói bảo hành.' : 'Đã tạo gói bảo hành.'); reset(); await load();
    } catch (e) { setError(extractApiError(e).message); }
  };
  return <div style={{ maxWidth: 1100, margin: '0 auto', padding: 24 }}>
    <h1>Gói bảo hành</h1><p>Quản lý danh mục gói được sử dụng khi cấp bảo hành.</p>
    {error && <ErrorMessage message={error} />}
    <section style={{ display: 'grid', gap: 10, padding: 16, border: '1px solid var(--color-border)', borderRadius: 8, marginBottom: 20 }}>
      <input placeholder="Tên gói" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
      <input placeholder="Mô tả" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
      <input type="number" min={1} placeholder="Số tháng" value={form.durationMonths} onChange={(e) => setForm({ ...form, durationMonths: Number(e.target.value) })} />
      <textarea placeholder="Điều khoản" value={form.terms} onChange={(e) => setForm({ ...form, terms: e.target.value })} />
      <div><Button onClick={save}>{editing ? 'Lưu thay đổi' : 'Thêm gói'}</Button>{editing && <Button variant="secondary" onClick={reset}>Hủy</Button>}</div>
    </section>
    {loading ? <Loading /> : <table style={{ width: '100%' }}><thead><tr><th>Tên</th><th>Thời hạn</th><th>Trạng thái</th><th /></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{item.name}<br /><small>{item.terms}</small></td><td>{item.durationMonths} tháng</td><td>{item.isActive ? 'Đang dùng' : 'Đã tắt'}</td><td><Button size="sm" variant="secondary" onClick={() => { setEditing(item.id); setForm({ name: item.name, description: item.description ?? '', durationMonths: item.durationMonths, terms: item.terms }); }}>Sửa</Button>{' '}<Button size="sm" variant={item.isActive ? 'danger' : 'primary'} onClick={async () => { try { await warrantyPackageService.toggle(item.id, !item.isActive); await load(); } catch (e) { setError(extractApiError(e).message); } }}>{item.isActive ? 'Tắt' : 'Bật'}</Button></td></tr>)}</tbody></table>}
  </div>;
}
