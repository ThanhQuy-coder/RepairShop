import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Smartphone,
  Laptop,
  Tablet,
  Watch,
  Cpu,
  Star,
  ShieldCheck,
  Clock,
  Wrench,
  Search,
  ArrowRight,
  CheckCircle2,
  FileSearch,
  Sparkles,
  CalendarCheck,
  Award,
  BookOpen,
} from '../../components/common/FixoraIcons';
import { Button, ErrorMessage, Loading } from '../../components/common';
import { contentService } from '../../services/contentService';
import { reviewService } from '../../services/reviewService';
import { deviceService } from '../../services/deviceService';
import { extractApiError } from '../../utils/apiError';
import type { ArticleListItem, ServiceItem } from '../../types/content.types';
import type { ReviewListItem } from '../../types/review.types';
import type { BrandItem } from '../../types/device.types';
import { DEVICE_TYPE_LABELS } from '../../types/device.types';
import { resolveServiceImageUrl, handleServiceImageError } from '../../utils/imageUrl';
import styles from './HomePage.module.css';

interface DeviceCategoryConfig {
  type: 'Phone' | 'Laptop' | 'Tablet' | 'Smartwatch' | 'Electronics';
  title: string;
  sectionTitle: string;
  subtitle: string;
  description: string;
  icon: React.ReactNode;
}

const DEVICE_CATEGORIES: DeviceCategoryConfig[] = [
  {
    type: 'Phone',
    title: 'Điện thoại',
    sectionTitle: 'Dịch vụ Sửa chữa Điện thoại',
    subtitle: 'iPhone, Samsung Galaxy, Xiaomi...',
    description:
      'Thay màn hình OLED/AMOLED, thay pin dung lượng chuẩn, ép kính, sửa Face ID, cụm camera và xử lý bo mạch điện thoại.',
    icon: <Smartphone size={26} />,
  },
  {
    type: 'Laptop',
    title: 'Laptop',
    sectionTitle: 'Dịch vụ Sửa chữa & Nâng cấp Laptop',
    subtitle: 'MacBook, Dell, ASUS, ThinkPad, HP...',
    description:
      'Thay màn hình, bàn phím, pin, sửa bản lề, nâng cấp SSD NVMe/RAM, vệ sinh tản nhiệt keo kim loại lỏng và sửa nguồn.',
    icon: <Laptop size={26} />,
  },
  {
    type: 'Tablet',
    title: 'Máy tính bảng',
    sectionTitle: 'Dịch vụ Sửa chữa Máy tính bảng',
    subtitle: 'iPad Pro, iPad Air, Galaxy Tab...',
    description:
      'Thay cảm ứng, ép kính màn hình lớn, thay pin iPad/Galaxy Tab, sửa chân sạc Type-C và nắn chỉnh khung vỏ cong vênh.',
    icon: <Tablet size={26} />,
  },
  {
    type: 'Smartwatch',
    title: 'Đồng hồ thông minh',
    sectionTitle: 'Dịch vụ Sửa chữa Đồng hồ thông minh',
    subtitle: 'Apple Watch, Galaxy Watch, Garmin...',
    description:
      'Ép kính cảm ứng cong, thay pin chuẩn dung lượng, phục hồi ron áp suất chống nước, sửa nút Digital Crown và cảm biến nhịp tim.',
    icon: <Watch size={26} />,
  },
  {
    type: 'Electronics',
    title: 'Thiết bị điện tử',
    sectionTitle: 'Chăm sóc & Sửa chữa Thiết bị điện tử',
    subtitle: 'AirPods, Sony, Marshall, Nintendo Switch, PS5...',
    description:
      'Thay pin tai nghe chống ồn, phục hồi mạch công suất loa Bluetooth, thay cần analog Hall Effect chống trôi và vệ sinh máy chơi game.',
    icon: <Cpu size={26} />,
  },
];

const REPAIR_WORKFLOW_STEPS = [
  {
    step: '01',
    title: 'Chọn dịch vụ',
    description:
      'Tra cứu bảng giá tham khảo minh bạch theo đúng loại thiết bị và tình trạng hư hỏng trên hệ thống Fixora.',
  },
  {
    step: '02',
    title: 'Đặt lịch hoặc gửi thiết bị',
    description:
      'Đặt lịch hẹn trực tuyến ưu tiên khung giờ hoặc mang trực tiếp thiết bị đến quầy tiếp nhận Fixora Care Desk.',
  },
  {
    step: '03',
    title: 'Tiếp nhận và kiểm tra',
    description:
      'Lễ tân và kỹ thuật viên đồng kiểm ngoại quan, ghi nhận serial/IMEI và chẩn đoán nguyên nhân lỗi miễn phí.',
  },
  {
    step: '04',
    title: 'Xác nhận báo giá',
    description:
      'Báo giá chi tiết từng hạng mục linh kiện và công thợ. Chỉ tiến hành sửa chữa khi khách hàng xác nhận đồng ý.',
  },
  {
    step: '05',
    title: 'Tiến hành sửa chữa',
    description:
      'Kỹ thuật viên chuyên môn thực hiện sửa chữa theo quy trình chuẩn; khách hàng theo dõi tiến độ thời gian thực.',
  },
  {
    step: '06',
    title: 'Kiểm tra và bàn giao',
    description:
      'Kiểm định chất lượng (QA) đa điểm trước khi bàn giao thiết bị và kích hoạt bảo hành điện tử tự động.',
  },
];

export default function HomePage() {
  const navigate = useNavigate();
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [articles, setArticles] = useState<ArticleListItem[]>([]);
  const [brands, setBrands] = useState<BrandItem[]>([]);
  const [heroSearch, setHeroSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    Promise.allSettled([
      contentService.getPublicServices(),
      reviewService.getPublicReviews(),
      contentService.getPublicArticles(),
      deviceService.getPublicBrands({ page: 1, pageSize: 20 }),
    ])
      .then(([serviceRes, reviewRes, articleRes, brandRes]) => {
        if (!isMounted) return;

        if (serviceRes.status === 'fulfilled') {
          setServices(serviceRes.value.items.filter((s) => s.isActive));
        }
        if (reviewRes.status === 'fulfilled') {
          setReviews(reviewRes.value.items);
        }
        if (articleRes.status === 'fulfilled') {
          setArticles(articleRes.value.items);
        }
        if (brandRes.status === 'fulfilled') {
          setBrands(brandRes.value.items.filter((b) => b.isActive));
        }

        if (serviceRes.status === 'rejected' && reviewRes.status === 'rejected') {
          setErrorMessage(extractApiError(serviceRes.reason).message);
        }
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const featuredServices = useMemo(() => {
    return [...services]
      .sort((a, b) => {
        const countDiff = (b.reviewCount ?? 0) - (a.reviewCount ?? 0);
        if (countDiff !== 0) return countDiff;
        return (b.averageRating ?? 0) - (a.averageRating ?? 0);
      })
      .slice(0, 8);
  }, [services]);

  const servicesByDeviceType = useMemo(() => {
    const grouped: Record<string, ServiceItem[]> = {};
    for (const cat of DEVICE_CATEGORIES) {
      grouped[cat.type] = services.filter(
        (s) => (s.deviceType ?? '').toLowerCase() === cat.type.toLowerCase()
      );
    }
    return grouped;
  }, [services]);

  const overallReviewStats = useMemo(() => {
    if (reviews.length === 0) {
      return { avg: 4.9, total: 0 };
    }
    const sum = reviews.reduce((acc, r) => acc + r.rating, 0);
    return {
      avg: Math.round((sum / reviews.length) * 10) / 10,
      total: reviews.length,
    };
  }, [reviews]);

  const handleHeroSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const q = heroSearch.trim();
    if (q) {
      navigate(`/services?search=${encodeURIComponent(q)}`);
    } else {
      navigate('/services');
    }
  };

  const renderServiceCard = (service: ServiceItem) => (
    <article className={styles.serviceCard} key={service.id}>
      <Link to={`/services/${service.id}`} className={styles.cardImageLink}>
        <div className={styles.cardImageWrap}>
          <img
            src={resolveServiceImageUrl(service.imageUrl, service.deviceType)}
            alt={service.name}
            className={styles.cardImage}
            loading="lazy"
            onError={handleServiceImageError}
          />
          <span className={styles.cardDeviceBadge}>
            {service.deviceType
              ? DEVICE_TYPE_LABELS[service.deviceType] || service.deviceType
              : 'Thiết bị'}
          </span>
        </div>
      </Link>

      <div className={styles.cardBody}>
        <div className={styles.cardMetaRow}>
          {service.reviewCount && service.reviewCount > 0 ? (
            <span className={styles.cardRating}>
              <Star size={13} className={styles.starIcon} />
              <strong>{(service.averageRating ?? 0).toFixed(1)}</strong>
              <span>({service.reviewCount})</span>
            </span>
          ) : (
            <span className={styles.cardWarrantyBadge}>
              <ShieldCheck size={13} /> Bảo hành chính hãng
            </span>
          )}
        </div>

        <h3 className={styles.cardTitle}>
          <Link to={`/services/${service.id}`}>{service.name}</Link>
        </h3>

        <p className={styles.cardDesc}>
          {service.description ??
            'Kiểm tra, chẩn đoán và thay thế linh kiện tiêu chuẩn tại Fixora.'}
        </p>

        <div className={styles.cardPriceRow}>
          <div>
            <span className={styles.cardPriceLabel}>Giá tham khảo</span>
            <strong className={styles.cardPrice}>
              {service.basePrice !== null
                ? `Từ ${service.basePrice.toLocaleString('vi-VN')}đ`
                : 'Liên hệ báo giá'}
            </strong>
          </div>
          <Link to={`/services/${service.id}`} className={styles.cardDetailLink}>
            Chi tiết <ArrowRight size={13} />
          </Link>
        </div>

        <Link
          to={`/book?serviceId=${service.id}${
            service.deviceType ? `&deviceType=${service.deviceType}` : ''
          }`}
          className={styles.cardBookLink}
        >
          <Button size="sm" className={styles.cardBookBtn}>
            Đặt lịch sửa chữa
          </Button>
        </Link>
      </div>
    </article>
  );

  return (
    <div className={styles.page}>
      {/* 1. HERO BANNER */}
      <section className={styles.heroWrapper}>
        <div className={styles.hero}>
          <div className={styles.heroCopy}>
            <div className={styles.brandBadge}>
              <span className={styles.pulseDot} />
              <span>FIXORA · YOUR DEVICES. OUR CARE.</span>
            </div>
            <h1>Trung tâm sửa chữa & bảo hành thiết bị công nghệ minh bạch.</h1>
            <p>
              Chẩn đoán chính xác, linh kiện chuẩn xuất xứ, báo giá trước khi làm và theo dõi tiến độ sửa chữa trực tuyến theo thời gian thực.
            </p>

            <form className={styles.heroSearchForm} onSubmit={handleHeroSearch}>
              <Search size={18} className={styles.heroSearchIcon} />
              <input
                type="text"
                value={heroSearch}
                onChange={(e) => setHeroSearch(e.target.value)}
                placeholder="Tìm dịch vụ cần sửa (VD: Thay màn hình iPhone, Thay pin MacBook, AirPods...)"
                aria-label="Tìm kiếm dịch vụ sửa chữa"
              />
              <button type="submit" className={styles.heroSearchBtn}>
                Tìm dịch vụ
              </button>
            </form>

            <div className={styles.heroActions}>
              <Link to="/book" className={styles.primaryCtaLink}>
                <Button size="lg">Đặt lịch sửa chữa</Button>
              </Link>
              <Link to="/services" className={styles.secondaryAction}>
                Khám phá dịch vụ <ArrowRight size={16} />
              </Link>
            </div>

            <div className={styles.heroTrustRow}>
              <span>
                <CheckCircle2 size={15} /> Kiểm tra máy miễn phí
              </span>
              <span>
                <CheckCircle2 size={15} /> Bảo hành điện tử 6–12 tháng
              </span>
              <span>
                <CheckCircle2 size={15} /> Không phát sinh chi phí ẩn
              </span>
            </div>
          </div>

          <div className={styles.heroVisual}>
            <div className={styles.heroDeskCard}>
              <div className={styles.deskHeader}>
                <div>
                  <span className={styles.deskBadge}>FIXORA CARE DESK</span>
                  <strong>Hệ thống chăm sóc thiết bị toàn diện</strong>
                </div>
                <span className={styles.liveIndicator}>
                  <span /> Online 24/7
                </span>
              </div>

              <div className={styles.deskVisualGrid}>
                <div className={styles.deskFeatureItem}>
                  <Wrench size={20} />
                  <div>
                    <strong>Kỹ thuật viên chuyên sâu</strong>
                    <span>Quy trình tháo lắp, ký tên linh kiện minh bạch</span>
                  </div>
                </div>
                <div className={styles.deskFeatureItem}>
                  <Clock size={20} />
                  <div>
                    <strong>Sửa nhanh lấy liền</strong>
                    <span>Thay pin, màn hình chỉ từ 45 – 90 phút</span>
                  </div>
                </div>
                <div className={styles.deskFeatureItem}>
                  <ShieldCheck size={20} />
                  <div>
                    <strong>Tra cứu tiến độ & Bảo hành</strong>
                    <span>Cập nhật từng bước qua mã phiếu và số Serial</span>
                  </div>
                </div>
              </div>

              <div className={styles.deskQuickLinks}>
                <Link to="/track" className={styles.deskQuickBtn}>
                  <FileSearch size={15} /> Tra cứu phiếu sửa
                </Link>
                <Link to="/consult" className={styles.deskQuickBtn}>
                  <Sparkles size={15} /> Trợ lý AI tư vấn
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 2. DANH MỤC 5 LOẠI THIẾT BỊ */}
      <section className={styles.categoriesSection}>
        <div className={styles.container}>
          <div className={styles.sectionHeader}>
            <div>
              <span className={styles.eyebrow}>DANH MỤC THIẾT BỊ</span>
              <h2>Chọn loại thiết bị bạn cần chăm sóc</h2>
            </div>
            <Link to="/services" className={styles.viewAllLink}>
              Tất cả dịch vụ <ArrowRight size={15} />
            </Link>
          </div>

          <div className={styles.categoryGrid}>
            {DEVICE_CATEGORIES.map((cat) => {
              const count = servicesByDeviceType[cat.type]?.length ?? 0;
              return (
                <Link
                  key={cat.type}
                  to={`/services?deviceType=${cat.type}`}
                  className={styles.categoryCard}
                >
                  <div className={styles.categoryIconBox}>{cat.icon}</div>
                  <div className={styles.categoryInfo}>
                    <h3>{cat.title}</h3>
                    <p>{cat.subtitle}</p>
                    <span className={styles.categoryCount}>
                      {count > 0 ? `${count} dịch vụ sẵn sàng` : 'Xem bảng giá'} →
                    </span>
                  </div>
                </Link>
              );
            })}
          </div>

          {brands.length > 0 && (
            <div className={styles.brandStrip}>
              <span className={styles.brandStripLabel}>Hỗ trợ đa hãng sản xuất:</span>
              <div className={styles.brandList}>
                {brands.slice(0, 12).map((brand) => (
                  <Link
                    key={brand.id}
                    to={`/services?search=${encodeURIComponent(brand.name)}`}
                    className={styles.brandChip}
                    title={`Tìm dịch vụ cho ${brand.name}`}
                  >
                    {brand.logoUrl && (
                      <img
                        src={brand.logoUrl}
                        alt={brand.name}
                        className={styles.brandChipLogo}
                        loading="lazy"
                      />
                    )}
                    <span>{brand.name}</span>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </section>

      {/* 3. DỊCH VỤ SỬA CHỮA NỔI BẬT */}
      <section className={styles.featuredSection}>
        <div className={styles.container}>
          <div className={styles.sectionHeader}>
            <div>
              <span className={styles.eyebrow}>DỊCH VỤ NỔI BẬT TẠI FIXORA</span>
              <h2>Giải pháp sửa chữa được khách hàng lựa chọn nhiều nhất</h2>
            </div>
            <Link to="/services" className={styles.viewAllLink}>
              Xem toàn bộ bảng giá <ArrowRight size={15} />
            </Link>
          </div>

          {isLoading && <Loading />}
          {errorMessage && <ErrorMessage message={errorMessage} />}

          {!isLoading && featuredServices.length > 0 && (
            <div className={styles.serviceGrid}>
              {featuredServices.map((service) => renderServiceCard(service))}
            </div>
          )}
        </div>
      </section>

      {/* 4. CÁC SECTION DỊCH VỤ THEO NHÓM THIẾT BỊ */}
      {!isLoading &&
        DEVICE_CATEGORIES.map((category, idx) => {
          const groupServices = servicesByDeviceType[category.type] ?? [];
          if (groupServices.length === 0) return null;

          return (
            <section
              key={category.type}
              className={idx % 2 === 0 ? styles.groupSectionAlt : styles.groupSection}
            >
              <div className={styles.container}>
                <div className={styles.groupBanner}>
                  <div className={styles.groupHeadingLeft}>
                    <div className={styles.groupIconBadge}>{category.icon}</div>
                    <div>
                      <span className={styles.eyebrow}>
                        CHUYÊN KHOA {category.title.toUpperCase()}
                      </span>
                      <h2>{category.sectionTitle}</h2>
                      <p>{category.description}</p>
                    </div>
                  </div>
                  <Link
                    to={`/services?deviceType=${category.type}`}
                    className={styles.viewAllGroupBtn}
                  >
                    Xem tất cả ({groupServices.length}) <ArrowRight size={15} />
                  </Link>
                </div>

                <div className={styles.serviceGrid}>
                  {groupServices.slice(0, 4).map((service) => renderServiceCard(service))}
                </div>
              </div>
            </section>
          );
        })}

      {/* 5. QUY TRÌNH SỬA CHỮA 6 BƯỚC */}
      <section className={styles.processSection}>
        <div className={styles.container}>
          <div className={styles.sectionHeaderCentered}>
            <span className={styles.eyebrow}>QUY TRÌNH TIÊU CHUẨN FIXORA</span>
            <h2>6 bước sửa chữa minh bạch — An tâm tuyệt đối</h2>
            <p>
              Mọi thiết bị tại Fixora đều tuân thủ quy trình kiểm tra, báo giá và giám sát chất lượng chặt chẽ trước khi bàn giao.
            </p>
          </div>

          <div className={styles.processGrid}>
            {REPAIR_WORKFLOW_STEPS.map((item) => (
              <div key={item.step} className={styles.processCard}>
                <div className={styles.processTop}>
                  <span className={styles.stepNumber}>Bước {item.step}</span>
                  <span className={styles.stepLine} />
                </div>
                <h3>{item.title}</h3>
                <p>{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* 6. ĐÁNH GIÁ KHÁCH HÀNG THỰC TẾ */}
      <section className={styles.reviewsSection}>
        <div className={styles.container}>
          <div className={styles.reviewsHeaderBar}>
            <div>
              <span className={styles.eyebrow}>ĐÁNH GIÁ KHÁCH HÀNG THỰC TẾ</span>
              <h2>Sự hài lòng được chứng thực qua từng ca sửa chữa</h2>
            </div>
            <div className={styles.overallScoreBadge}>
              <div className={styles.scoreNumber}>{overallReviewStats.avg.toFixed(1)}</div>
              <div>
                <div className={styles.starsRow}>
                  {[1, 2, 3, 4, 5].map((s) => (
                    <Star key={s} size={15} className={styles.starIcon} />
                  ))}
                </div>
                <span>
                  {overallReviewStats.total > 0
                    ? `${overallReviewStats.total} đánh giá xác thực từ phiếu sửa chữa`
                    : 'Đánh giá xác thực từ khách hàng Fixora'}
                </span>
              </div>
            </div>
          </div>

          {reviews.length > 0 ? (
            <div className={styles.reviewGrid}>
              {reviews.slice(0, 6).map((review) => (
                <blockquote key={review.id} className={styles.reviewCard}>
                  <div className={styles.reviewCardTop}>
                    <div className={styles.starsRow}>
                      {[1, 2, 3, 4, 5].map((s) => (
                        <Star
                          key={s}
                          size={14}
                          className={
                            s <= review.rating ? styles.starIcon : styles.starMuted
                          }
                        />
                      ))}
                    </div>
                    <span className={styles.verifiedTag}>
                      <CheckCircle2 size={12} /> Đã sửa chữa
                    </span>
                  </div>
                  <p className={styles.reviewText}>
                    “{review.comment ?? 'Dịch vụ sửa chữa tận tâm, minh bạch và đúng hẹn.'}”
                  </p>
                  <footer className={styles.reviewFooter}>
                    <div className={styles.reviewerAvatar}>
                      {(review.customerName || 'K').charAt(0).toUpperCase()}
                    </div>
                    <div>
                      <cite>{review.customerName}</cite>
                      <time>
                        {new Date(review.createdAt).toLocaleDateString('vi-VN', {
                          day: '2-digit',
                          month: '2-digit',
                          year: 'numeric',
                        })}
                      </time>
                    </div>
                  </footer>
                </blockquote>
              ))}
            </div>
          ) : (
            <div className={styles.reviewEmptyNote}>
              Đánh giá từ khách hàng sẽ được cập nhật tự động ngay khi phiếu sửa chữa hoàn tất.
            </div>
          )}
        </div>
      </section>

      {/* 7. TRA CỨU BẢO HÀNH & LỊCH SỬ SỬA CHỮA */}
      <section className={styles.lookupSection}>
        <div className={styles.container}>
          <div className={styles.lookupBanner}>
            <div className={styles.lookupCopy}>
              <span className={styles.lookupEyebrow}>FIXORA DIGITAL CARE</span>
              <h2>Tra cứu tiến độ sửa chữa, lịch hẹn & bảo hành điện tử</h2>
              <p>
                Không cần giữ phiếu giấy. Chỉ với mã phiếu sửa chữa, mã lịch hẹn hoặc tài khoản khách hàng, bạn có thể kiểm tra tình trạng thiết bị và thời hạn bảo hành mọi lúc, mọi nơi.
              </p>
            </div>

            <div className={styles.lookupCardsGrid}>
              <Link to="/track" className={styles.lookupActionCard}>
                <div className={styles.lookupCardIcon}>
                  <FileSearch size={22} />
                </div>
                <div>
                  <h3>Tra cứu tiến độ sửa chữa</h3>
                  <p>Kiểm tra trạng thái tiếp nhận, báo giá và hoàn tất theo mã phiếu.</p>
                  <span>Tra cứu ngay →</span>
                </div>
              </Link>

              <Link to="/appointments/lookup" className={styles.lookupActionCard}>
                <div className={styles.lookupCardIcon}>
                  <CalendarCheck size={22} />
                </div>
                <div>
                  <h3>Tra cứu lịch hẹn</h3>
                  <p>Xem lại thông tin khung giờ đã đặt hoặc cập nhật lịch hẹn sửa chữa.</p>
                  <span>Kiểm tra lịch hẹn →</span>
                </div>
              </Link>

              <Link to="/consult" className={styles.lookupActionCard}>
                <div className={styles.lookupCardIcon}>
                  <Sparkles size={22} />
                </div>
                <div>
                  <h3>Trợ lý AI: Chẩn đoán, Báo giá & Đặt lịch</h3>
                  <p>Mô tả triệu chứng lỗi thiết bị để nhận chẩn đoán sơ bộ, chi phí dự kiến và gợi ý khung giờ.</p>
                  <span>Thử trợ lý AI →</span>
                </div>
              </Link>

              <Link to="/login" className={styles.lookupActionCard}>
                <div className={styles.lookupCardIcon}>
                  <Award size={22} />
                </div>
                <div>
                  <h3>Lịch sử sửa chữa & Bảo hành</h3>
                  <p>Đăng nhập để xem toàn bộ lịch sử thiết bị và thời hạn bảo hành điện tử.</p>
                  <span>Truy cập tài khoản →</span>
                </div>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* 8. BLOG / TIN TỨC & CẨM NANG THIẾT BỊ */}
      {articles.length > 0 && (
        <section className={styles.blogSection}>
          <div className={styles.container}>
            <div className={styles.sectionHeader}>
              <div>
                <span className={styles.eyebrow}>GÓC KỸ THUẬT & CẨM NANG FIXORA</span>
                <h2>Kinh nghiệm sử dụng và bảo quản thiết bị bền bỉ</h2>
              </div>
              <Link to="/articles" className={styles.viewAllLink}>
                Xem tất cả bài viết <ArrowRight size={15} />
              </Link>
            </div>

            <div className={styles.blogGrid}>
              {articles.slice(0, 3).map((article) => (
                <Link
                  key={article.id}
                  to={`/articles/${article.id}`}
                  className={styles.blogCard}
                >
                  <div className={styles.blogTopRow}>
                    <span className={styles.blogTag}>
                      <BookOpen size={13} /> Cẩm nang Fixora
                    </span>
                    <time>
                      {new Date(article.createdAt).toLocaleDateString('vi-VN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                      })}
                    </time>
                  </div>
                  <h3>{article.title}</h3>
                  <p>
                    Chia sẻ kiến thức chẩn đoán, kinh nghiệm bảo quản thiết bị và chính sách chăm sóc khách hàng tại Fixora.
                  </p>
                  <span className={styles.blogReadMore}>
                    Đọc bài viết <ArrowRight size={14} />
                  </span>
                </Link>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* 9. FINAL CTA */}
      <section className={styles.bottomCtaSection}>
        <div className={styles.container}>
          <div className={styles.bottomCtaBox}>
            <span className={styles.eyebrow}>YOUR DEVICES. OUR CARE.</span>
            <h2>Thiết bị của bạn đang gặp sự cố?</h2>
            <p>
              Đặt lịch hẹn trước tại Fixora để được ưu tiên tiếp nhận, kiểm tra lỗi miễn phí và nhận tư vấn trực tiếp từ kỹ thuật viên trưởng.
            </p>
            <div className={styles.bottomCtaButtons}>
              <Link to="/book">
                <Button size="lg">Đặt lịch sửa chữa ngay</Button>
              </Link>
              <Link to="/services" className={styles.bottomSecondaryBtn}>
                Xem bảng giá dịch vụ
              </Link>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
