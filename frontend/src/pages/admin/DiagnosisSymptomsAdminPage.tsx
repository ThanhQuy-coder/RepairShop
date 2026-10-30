import { useEffect, useState } from 'react';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { diagnosisService } from '../../services/diagnosisService';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';
import type { DiagnosisSymptom, DeviceType } from '../../types/diagnosis.types';
import styles from './DiagnosisSymptomsAdminPage.module.css';

const deviceTypes: DeviceType[] = ['Phone', 'Laptop', 'Electronics', 'Tablet', 'Smartwatch'];

export default function DiagnosisSymptomsAdminPage() {
  const { showSuccess } = useToast();
  const [items, setItems] = useState<DiagnosisSymptom[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [deviceType, setDeviceType] = useState<DeviceType>('Phone');
  const [sortOrder, setSortOrder] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    try {
      setItems(await diagnosisService.listAdmin());
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const reset = () => {
    setEditingId(null);
    setName('');
    setDeviceType('Phone');
    setSortOrder(1);
  };

  const save = async () => {
    if (!name.trim()) {
      setErrorMessage('Vui lòng nhập tên triệu chứng.');
      return;
    }
    try {
      const payload = { name: name.trim(), deviceType, sortOrder };
      if (editingId) await diagnosisService.update(editingId, payload);
      else await diagnosisService.create(payload);
      showSuccess(editingId ? 'Đã cập nhật triệu chứng.' : 'Đã thêm triệu chứng.');
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
  };

  const toggle = async (item: DiagnosisSymptom) => {
    try {
      await diagnosisService.toggleActive(item.id, !item.isActive);
      showSuccess(item.isActive ? 'Đã tắt triệu chứng.' : 'Đã bật triệu chứng.');
      await load();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.eyebrow}>CẤU HÌNH NGHIỆP VỤ</span>
        <h1 className={styles.title}>Danh mục triệu chứng chẩn đoán</h1>
        <p className={styles.subtitle}>Quản lý checklist theo loại thiết bị cho kỹ thuật viên.</p>
      </div>
      {errorMessage && <ErrorMessage message={errorMessage} />}
      <section className={styles.form}>
        <h3>{editingId ? 'Chỉnh sửa triệu chứng' : 'Thêm triệu chứng'}</h3>
        <div className={styles.formGrid}>
          <label>Tên triệu chứng<input value={name} onChange={(e) => setName(e.target.value)} /></label>
          <label>Loại thiết bị<select value={deviceType} onChange={(e) => setDeviceType(e.target.value as DeviceType)}>
            {deviceTypes.map((type) => <option key={type}>{type}</option>)}
          </select></label>
          <label>Thứ tự<input type="number" min={0} value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value))} /></label>
        </div>
        <div className={styles.actions}>
          <Button onClick={save}>{editingId ? 'Lưu thay đổi' : 'Thêm mới'}</Button>
          {editingId && <Button variant="secondary" onClick={reset}>Hủy</Button>}
        </div>
      </section>
      {isLoading ? <Loading /> : (
        <div className={styles.tableWrap}>
          <table>
            <thead><tr><th>Triệu chứng</th><th>Thiết bị</th><th>Thứ tự</th><th>Trạng thái</th><th /></tr></thead>
            <tbody>{items.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td><td>{item.deviceType}</td><td>{item.sortOrder}</td>
                <td>{item.isActive ? 'Đang dùng' : 'Đã tắt'}</td>
                <td className={styles.rowActions}>
                  <Button size="sm" variant="secondary" onClick={() => edit(item)}>Sửa</Button>
                  <Button size="sm" variant={item.isActive ? 'danger' : 'primary'} onClick={() => toggle(item)}>
                    {item.isActive ? 'Tắt' : 'Bật'}
                  </Button>
                </td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </div>
  );
}
