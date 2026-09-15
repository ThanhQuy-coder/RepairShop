import { useEffect, useState } from 'react';
import { Button, Input } from '../../components/common';
import { useAuth } from '../../hooks/useAuth';
import { useToastStore } from '../../store/toastStore';
import { userService } from '../../services/userService';
import styles from './CustomerProfilePage.module.css';

export default function CustomerProfilePage() {
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

    loadProfile();
  }, [addToast]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    // Hiện tại API chỉ có getMyProfile nên chưa có endpoint cập nhật.
    addToast('info', 'Chức năng cập nhật hồ sơ sẽ được kết nối với API sau.');
  };

  const avatarLetter = (fullName || user?.email || 'U')[0].toUpperCase();

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>TÀI KHOẢN CỦA BẠN</span>
          <h1 className={styles.title}>Hồ sơ người dùng</h1>
          <p className={styles.subtitle}>
            Quản lý thông tin liên hệ để RepairShop dễ dàng cập nhật tiến độ cho bạn.
          </p>
        </div>

        <div className={styles.avatar}>{avatarLetter}</div>
      </header>

      <div className={styles.layout}>
        <aside className={styles.identityCard}>
          <div className={styles.largeAvatar}>{avatarLetter}</div>

          <h2>{fullName || 'Khách hàng RepairShop'}</h2>
          <p>{user?.email}</p>

          <span className={styles.accountTag}>Tài khoản khách hàng</span>

          <div className={styles.identityNote}>
            <strong>Bảo mật tài khoản</strong>
            <span>Email đăng nhập được dùng để xác nhận các thông báo sửa chữa.</span>
          </div>
        </aside>

        <form className={styles.formCard} onSubmit={handleSubmit}>
          <div className={styles.formHeader}>
            <div>
              <span className={styles.sectionKicker}>THÔNG TIN CÁ NHÂN</span>
              <h2>Thông tin liên hệ</h2>
            </div>

            <span className={styles.savedDot}>● Đang hoạt động</span>
          </div>

          <div className={styles.formGrid}>
            <Input
              label="Họ và tên"
              placeholder="Nhập họ và tên"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              disabled={loading}
            />

            <Input
              label="Số điện thoại"
              placeholder="Nhập số điện thoại"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              disabled={loading}
            />

            <div className={styles.fullWidth}>
              <Input label="Email đăng nhập" value={user?.email || ''} disabled />
            </div>

            <div className={styles.fullWidth}>
              <label className={styles.label} htmlFor="address">
                Địa chỉ
              </label>

              <textarea
                id="address"
                className={styles.textarea}
                placeholder="Nhập địa chỉ nhận thiết bị"
                value={address}
                onChange={(event) => setAddress(event.target.value)}
                rows={4}
              />
            </div>
          </div>

          <div className={styles.formFooter}>
            <span>Thông tin hồ sơ được lấy từ tài khoản của bạn.</span>

            <Button type="submit" disabled={loading}>
              Lưu thay đổi
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
