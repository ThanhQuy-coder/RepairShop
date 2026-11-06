import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input } from '../../components/common';
import { useAuth } from '../../hooks/useAuth';
import { useToastStore } from '../../store/toastStore';
import { userService } from '../../services/userService';
import styles from './CustomerProfilePage.module.css';

export default function CustomerProfilePage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const addToast = useToastStore((state) => state.addToast);

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');

  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadProfile = async () => {
      try {
        const profile = await userService.getMyProfile();

        setFullName(profile.fullName || '');
        setPhone(profile.phone || '');
      } catch {
        addToast('error', 'Không thể tải thông tin hồ sơ.');
      } finally {
        setLoading(false);
      }
    };

    void loadProfile();
  }, [addToast]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const trimmedName = fullName.trim();
    const trimmedPhone = phone.trim();

    if (!trimmedName) {
      addToast('error', 'Vui lòng nhập họ và tên.');
      return;
    }

    if (!trimmedPhone) {
      addToast('error', 'Vui lòng nhập số điện thoại.');
      return;
    }

    try {
      setLoading(true);
      const updated = await userService.updateMyProfile({
        fullName: trimmedName,
        phone: trimmedPhone,
      });

      setFullName(updated.fullName || '');
      setPhone(updated.phone || '');
      addToast('success', 'Cập nhật hồ sơ Fixora thành công.');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Không thể cập nhật hồ sơ.';
      addToast('error', message);
    } finally {
      setLoading(false);
    }
  };

  const avatarLetter = (fullName || user?.email || 'U')[0].toUpperCase();

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>HỒ SƠ THÀNH VIÊN — FIXORA</span>
          <h1 className={styles.title}>Hồ sơ người dùng</h1>
          <p className={styles.subtitle}>
            Quản lý thông tin liên hệ để Fixora dễ dàng đồng bộ phiếu sửa chữa, lịch hẹn và bảo hành điện tử cho bạn.
          </p>
        </div>

        <div className={styles.avatar}>{avatarLetter}</div>
      </header>

      <div className={styles.layout}>
        <aside className={styles.identityCard}>
          <div className={styles.largeAvatar}>{avatarLetter}</div>

          <h2>{fullName || 'Khách hàng Fixora'}</h2>
          <p>{user?.email}</p>

          <span className={styles.accountTag}>✓ Thành viên Fixora Care</span>

          <div className={styles.quickNavList}>
            <button
              type="button"
              className={styles.quickNavBtn}
              onClick={() => navigate('/customer/my-tickets')}
            >
              <span>📋 Phiếu sửa chữa của tôi</span>
              <span>→</span>
            </button>
            <button
              type="button"
              className={styles.quickNavBtn}
              onClick={() => navigate('/customer/appointments')}
            >
              <span>📅 Lịch hẹn của tôi</span>
              <span>→</span>
            </button>
            <button
              type="button"
              className={styles.quickNavBtn}
              onClick={() => navigate('/customer/warranty')}
            >
              <span>🛡️ Chứng nhận bảo hành</span>
              <span>→</span>
            </button>
            <button
              type="button"
              className={styles.quickNavBtn}
              onClick={() => navigate('/book')}
            >
              <span>🔧 Đặt lịch sửa mới</span>
              <span>→</span>
            </button>
          </div>

          <div className={styles.identityNote}>
            <strong>🔒 Bảo mật &amp; Đồng bộ tự động</strong>
            <span>
              Số điện thoại và email của bạn được dùng để tự động liên kết lịch sử sửa chữa và kích hoạt bảo hành điện tử tại hệ thống Fixora.
            </span>
          </div>
        </aside>

        <form className={styles.formCard} onSubmit={handleSubmit}>
          <div className={styles.formHeader}>
            <div>
              <span className={styles.sectionKicker}>THÔNG TIN CÁ NHÂN</span>
              <h2>Thông tin liên hệ &amp; Nhận thiết bị</h2>
            </div>

            <span className={styles.savedDot}>● Đang hoạt động</span>
          </div>

          <div className={styles.formGrid}>
            <Input
              label="Họ và tên *"
              placeholder="Nhập họ và tên đầy đủ"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              disabled={loading}
            />

            <Input
              label="Số điện thoại liên hệ *"
              placeholder="Nhập số điện thoại (VD: 0901234567)"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              disabled={loading}
            />

            <div className={styles.fullWidth}>
              <Input label="Email đăng nhập (Định danh tài khoản)" value={user?.email || ''} disabled />
            </div>

            <div className={styles.fullWidth}>
              <label className={styles.label} htmlFor="address">
                Địa chỉ liên hệ / Giao nhận thiết bị (Tùy chọn)
              </label>

              <textarea
                id="address"
                className={styles.textarea}
                placeholder="Nhập địa chỉ nhận thiết bị hoặc ghi chú liên hệ..."
                value={address}
                onChange={(event) => setAddress(event.target.value)}
                rows={3}
              />
            </div>
          </div>

          <div className={styles.formFooter}>
            <span>Thông tin hồ sơ được bảo mật theo chính sách khách hàng của Fixora.</span>

            <Button type="submit" isLoading={loading} disabled={loading}>
              Lưu thay đổi
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
