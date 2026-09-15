interface PlaceholderPageProps {
  title: string;
}

import { Link } from 'react-router-dom';
import { Button, Badge } from '../components/common';
import { useAuth } from '../hooks/useAuth';
import styles from './PlaceholderPage.module.css';

export default function PlaceholderPage({ title }: PlaceholderPageProps) {
  const { email, role, logout } = useAuth();

  if (title === 'Trang chủ') {
    return (
      <div className={styles.homePage}>
        <section className={styles.hero}>
          <div className={styles.heroCopy}>
            <p className={styles.eyebrow}>REPAIRSHOP / CARE DESK</p>
            <h1>Sửa chữa rõ ràng, thiết bị trở lại đúng hẹn.</h1>
            <p className={styles.heroText}>
              Theo dõi tình trạng thiết bị, nhận cập nhật tiến độ và kết nối với đội ngũ kỹ thuật
              trong một quy trình minh bạch.
            </p>
            <div className={styles.heroActions}>
              <Link to="/track">
                <Button>Tra cứu phiếu sửa chữa</Button>
              </Link>
              <Link to="/register" className={styles.textAction}>
                Tạo tài khoản khách hàng <span aria-hidden="true">→</span>
              </Link>
            </div>
          </div>
          <div className={styles.heroPanel} aria-label="Thông tin quy trình sửa chữa">
            <div className={styles.panelTopline}>
              <span className={styles.liveDot} /> Đang vận hành
            </div>
            <div className={styles.panelNumber}>01</div>
            <p>Minh bạch từ lúc tiếp nhận đến khi bàn giao</p>
            <div className={styles.progressLine}>
              <span />
            </div>
            <div className={styles.panelMeta}>
              <span>Tiếp nhận</span>
              <span>Hoàn tất</span>
            </div>
          </div>
        </section>

        <section className={styles.serviceStrip}>
          <div>
            <strong>01</strong>
            <span>Gửi thiết bị</span>
            <small>Tiếp nhận & ghi nhận tình trạng</small>
          </div>
          <div>
            <strong>02</strong>
            <span>Kiểm tra</span>
            <small>Chẩn đoán bởi kỹ thuật viên</small>
          </div>
          <div>
            <strong>03</strong>
            <span>Nhận cập nhật</span>
            <small>Luôn biết thiết bị đang ở đâu</small>
          </div>
        </section>

        <section className={styles.homeSection}>
          <div className={styles.sectionIntro}>
            <p className={styles.eyebrow}>MỘT NƠI ĐỂ THEO DÕI</p>
            <h2>Mọi thông tin sửa chữa, ở ngay trong tầm tay.</h2>
          </div>
          <div className={styles.featureGrid}>
            <article>
              <span className={styles.featureIndex}>A/</span>
              <h3>Tra cứu tức thì</h3>
              <p>Dùng mã phiếu để xem trạng thái mới nhất mà không cần gọi điện chờ đợi.</p>
            </article>
            <article>
              <span className={styles.featureIndex}>B/</span>
              <h3>Lịch sử rõ ràng</h3>
              <p>Từng mốc xử lý được ghi nhận để bạn an tâm về thiết bị của mình.</p>
            </article>
            <article>
              <span className={styles.featureIndex}>C/</span>
              <h3>Hỗ trợ đúng lúc</h3>
              <p>Kết nối với đội ngũ RepairShop khi cần tư vấn hoặc xác nhận dịch vụ.</p>
            </article>
          </div>
        </section>
      </div>
    );
  }

  if (title === 'Trang chủ khách hàng') {
    return (
      <div className={styles.customerPortal}>
        <header className={styles.portalHeader}>
          <span className={styles.portalEyebrow}>CỔNG THÔNG TIN KHÁCH HÀNG</span>
          <h1 className={styles.portalTitle}>Xin chào, {email ?? 'Quý khách'}!</h1>
          <p className={styles.portalSubtitle}>
            Theo dõi tiến độ sửa chữa, tra cứu bảo hành và quản lý thiết bị của bạn tại một nơi tập
            trung
          </p>
        </header>

        <div className={styles.actionGrid}>
          <Link to="/customer/my-tickets" className={styles.actionCard}>
            <div className={styles.actionCardIcon}>🔧</div>
            <div className={styles.actionCardTitle}>Phiếu sửa chữa của tôi</div>
            <div className={styles.actionCardDesc}>
              Xem trạng thái thời gian thực, tiến độ tiếp nhận, báo giá và nhật ký kỹ thuật của các
              thiết bị gửi sửa.
            </div>
            <div className={styles.actionCardLink}>
              Truy cập phiếu sửa chữa <span>→</span>
            </div>
          </Link>

          <Link to="/customer/warranty" className={styles.actionCard}>
            <div className={styles.actionCardIcon}>🛡️</div>
            <div className={styles.actionCardTitle}>Bảo hành thiết bị</div>
            <div className={styles.actionCardDesc}>
              Kiểm tra thời hạn bảo hành linh kiện, trạng thái hiệu lực và gửi yêu cầu bảo hành trực
              tuyến nhanh chóng.
            </div>
            <div className={styles.actionCardLink}>
              Xem danh sách bảo hành <span>→</span>
            </div>
          </Link>

          <Link to="/track" className={styles.actionCard}>
            <div className={styles.actionCardIcon}>🔍</div>
            <div className={styles.actionCardTitle}>Tra cứu nhanh</div>
            <div className={styles.actionCardDesc}>
              Nhập mã biên nhận sửa chữa để tra cứu công khai tiến độ bàn giao bất kỳ lúc nào mà
              không cần đăng nhập lại.
            </div>
            <div className={styles.actionCardLink}>
              Mở trang tra cứu <span>→</span>
            </div>
          </Link>
        </div>

        <div className={styles.tipBanner}>
          <h3>Cam kết chất lượng dịch vụ RepairShop</h3>
          <ul className={styles.tipList}>
            <li className={styles.tipItem}>
              <span>⚡</span>
              <div>
                <strong>Tiếp nhận tức thì:</strong> Biên nhận rõ ràng, thông báo qua hệ thống ngay
                khi kiểm tra xong.
              </div>
            </li>
            <li className={styles.tipItem}>
              <span>🛡️</span>
              <div>
                <strong>Linh kiện chính hãng:</strong> Bảo hành điện tử rõ ràng, tra cứu mã số dễ
                dàng 24/7.
              </div>
            </li>
            <li className={styles.tipItem}>
              <span>🤝</span>
              <div>
                <strong>Báo giá minh bạch:</strong> Kỹ thuật viên chỉ tiến hành sửa chữa sau khi quý
                khách đồng thuận.
              </div>
            </li>
          </ul>
        </div>
      </div>
    );
  }

  if (title === 'Hồ sơ') {
    const initial = (email?.[0] ?? 'U').toUpperCase();
    return (
      <div className={styles.profileContainer}>
        <header className={styles.portalHeader}>
          <span className={styles.portalEyebrow}>TÀI KHOẢN CÁ NHÂN</span>
          <h1 className={styles.portalTitle}>Hồ sơ người dùng</h1>
          <p className={styles.portalSubtitle}>Thông tin định danh và cài đặt tài khoản của bạn</p>
        </header>

        <div className={styles.profileCard}>
          <div className={styles.profileHeader}>
            <div className={styles.profileAvatar}>{initial}</div>
            <div className={styles.profileMeta}>
              <div className={styles.profileEmail}>{email ?? 'Chưa cập nhật email'}</div>
              <div>
                <Badge variant="info">{role ?? 'Customer'}</Badge>
              </div>
            </div>
          </div>

          <div className={styles.profileDetails}>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Email đăng nhập:</span>
              <span className={styles.detailValue}>{email}</span>
            </div>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Vai trò hệ thống:</span>
              <span className={styles.detailValue}>{role}</span>
            </div>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Trạng thái phiên đăng nhập:</span>
              <span
                className={styles.detailValue}
                style={{ color: 'var(--color-success, #1b8a5a)' }}
              >
                ● Đang hoạt động
              </span>
            </div>
          </div>

          <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
            <Link to="/customer/my-tickets">
              <Button variant="secondary">Xem phiếu sửa chữa</Button>
            </Link>
            <Button variant="danger" onClick={logout}>
              Đăng xuất
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.generic}>
      <h2>{title}</h2>
      <p>Trang đang được xây dựng.</p>
    </div>
  );
}
