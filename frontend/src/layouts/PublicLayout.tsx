import { type FormEvent, useEffect, useState } from 'react';
import { Outlet, Link, NavLink, useNavigate } from 'react-router-dom';
import { Search, CalendarPlus, User, Wrench, PhoneCall, Mail, MapPin, Clock } from '../components/common/FixoraIcons';
import { PUBLIC_NAV } from '../constants/navigation';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/common';
import styles from './PublicLayout.module.css';

export default function PublicLayout() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [headerSearch, setHeaderSearch] = useState('');

  const closeMenu = () => setIsMenuOpen(false);

  useEffect(() => {
    if (!isMenuOpen) return;
    const onClick = (e: MouseEvent) => {
      if (!(e.target as Element).closest(`.${styles.navbar}`)) {
        setIsMenuOpen(false);
      }
    };
    document.addEventListener('click', onClick);
    return () => document.removeEventListener('click', onClick);
  }, [isMenuOpen]);

  const handleHeaderSearch = (e: FormEvent) => {
    e.preventDefault();
    const q = headerSearch.trim();
    closeMenu();
    if (q) {
      navigate(`/services?search=${encodeURIComponent(q)}`);
    } else {
      navigate('/services');
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.navbar}>
        <div className={styles.navInner}>
          <Link to="/" className={styles.logo} onClick={closeMenu}>
            <span className={styles.logoMark} aria-hidden="true">
              <Wrench size={18} />
            </span>
            <span className={styles.logoTextWrap}>
              <strong className={styles.logoTitle}>FIXORA</strong>
              <span className={styles.logoTagline}>Your Devices. Our Care.</span>
            </span>
          </Link>

          <form className={styles.headerSearch} onSubmit={handleHeaderSearch}>
            <Search size={15} className={styles.searchIcon} />
            <input
              type="text"
              value={headerSearch}
              onChange={(e) => setHeaderSearch(e.target.value)}
              placeholder="Tìm dịch vụ sửa chữa..."
              aria-label="Tìm kiếm dịch vụ sửa chữa"
            />
          </form>

          <nav id="public-nav" className={`${styles.nav} ${isMenuOpen ? styles.navOpen : ''}`}>
            {PUBLIC_NAV.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.path === '/'}
                className={({ isActive }) =>
                  `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
                }
                onClick={closeMenu}
              >
                {item.label}
              </NavLink>
            ))}

            <div className={styles.mobileMenuSearch}>
              <form onSubmit={handleHeaderSearch} className={styles.mobileSearchForm}>
                <Search size={15} />
                <input
                  type="text"
                  value={headerSearch}
                  onChange={(e) => setHeaderSearch(e.target.value)}
                  placeholder="Tìm dịch vụ sửa chữa..."
                />
              </form>
            </div>
          </nav>

          <div className={styles.cta}>
            <Link to="/book" className={styles.bookBtnLink} onClick={closeMenu}>
              <Button size="sm" variant="secondary" className={styles.bookHeaderBtn}>
                <CalendarPlus size={15} /> <span>Đặt lịch</span>
              </Button>
            </Link>

            {isAuthenticated ? (
              <Link to="/dashboard" className={styles.authLink} onClick={closeMenu}>
                <Button size="sm">
                  <User size={15} /> <span>Tài khoản</span>
                </Button>
              </Link>
            ) : (
              <Link to="/login" className={styles.authLink} onClick={closeMenu}>
                <Button size="sm">Đăng nhập</Button>
              </Link>
            )}
          </div>

          <button
            type="button"
            className={styles.hamburger}
            onClick={() => setIsMenuOpen((prev) => !prev)}
            aria-label={isMenuOpen ? 'Đóng menu điều hướng' : 'Mở menu điều hướng'}
            aria-expanded={isMenuOpen}
            aria-controls="public-nav"
          >
            {isMenuOpen ? (
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            ) : (
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <line x1="3" y1="6" x2="21" y2="6" />
                <line x1="3" y1="12" x2="21" y2="12" />
                <line x1="3" y1="18" x2="21" y2="18" />
              </svg>
            )}
          </button>
        </div>
      </header>

      <main className={styles.content}>
        <Outlet />
      </main>

      <footer className={styles.footer}>
        <div className={styles.footerContainer}>
          <div className={styles.footerBrandCol}>
            <Link to="/" className={styles.footerLogo}>
              <span className={styles.footerLogoMark}>
                <Wrench size={18} />
              </span>
              <div>
                <strong>FIXORA</strong>
                <span>Your Devices. Our Care.</span>
              </div>
            </Link>
            <p className={styles.footerDesc}>
              Hệ thống sửa chữa và bảo hành thiết bị công nghệ chuyên nghiệp. Cam kết minh bạch quy trình, linh kiện chuẩn xác và theo dõi tiến độ sửa chữa trực tuyến theo thời gian thực.
            </p>
          </div>

          <div className={styles.footerCol}>
            <h4>Dịch vụ sửa chữa</h4>
            <ul>
              <li>
                <Link to="/services?deviceType=Phone">Sửa chữa Điện thoại</Link>
              </li>
              <li>
                <Link to="/services?deviceType=Laptop">Sửa chữa & Nâng cấp Laptop</Link>
              </li>
              <li>
                <Link to="/services?deviceType=Tablet">Sửa chữa Máy tính bảng</Link>
              </li>
              <li>
                <Link to="/services?deviceType=Smartwatch">Sửa chữa Đồng hồ thông minh</Link>
              </li>
              <li>
                <Link to="/services?deviceType=Electronics">Chăm sóc Thiết bị điện tử</Link>
              </li>
            </ul>
          </div>

          <div className={styles.footerCol}>
            <h4>Hỗ trợ & Chính sách</h4>
            <ul>
              <li>
                <Link to="/book">Đặt lịch hẹn sửa chữa</Link>
              </li>
              <li>
                <Link to="/track">Tra cứu tiến độ phiếu sửa</Link>
              </li>
              <li>
                <Link to="/appointments/lookup">Tra cứu lịch hẹn trực tuyến</Link>
              </li>
              <li>
                <Link to="/consult">Trợ lý AI: Chẩn đoán, Báo giá & Đặt lịch</Link>
              </li>
              <li>
                <Link to="/estimate">Dự toán chi phí AI (cơ bản)</Link>
              </li>
              <li>
                <Link to="/articles">Cẩm nang kỹ thuật & Bảo hành</Link>
              </li>
            </ul>
          </div>

          <div className={styles.footerCol}>
            <h4>Thông tin liên hệ</h4>
            <ul className={styles.contactList}>
              <li>
                <PhoneCall size={15} />
                <span>Hotline: 1900 6868 (Miễn phí tư vấn)</span>
              </li>
              <li>
                <Mail size={15} />
                <span>support@fixora.vn</span>
              </li>
              <li>
                <MapPin size={15} />
                <span>Trung tâm Fixora Care Desk — TP. Hồ Chí Minh</span>
              </li>
              <li>
                <Clock size={15} />
                <span>08:00 – 20:30 (Thứ 2 – Chủ nhật)</span>
              </li>
            </ul>
          </div>
        </div>

        <div className={styles.footerBottom}>
          <p>© 2026 Fixora — Your Devices. Our Care. Hệ thống quản lý sửa chữa và bảo hành thiết bị.</p>
        </div>
      </footer>
    </div>
  );
}
