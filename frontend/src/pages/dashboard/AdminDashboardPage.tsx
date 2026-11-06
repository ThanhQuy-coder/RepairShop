import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { reportsService } from '../../services/reportsService';
import { extractApiError } from '../../utils/apiError';
import type {
  DashboardSummary,
  ProfitReport,
  SlaAnalytics,
} from '../../types/reports.types';
import { Loading, ErrorMessage, Button, Badge } from '../../components/common';
import {
  Wrench,
  Smartphone,
  ShieldCheck,
  FileSearch,
  BookOpen,
  Star,
  Clock,
  User,
  ArrowRight,
} from '../../components/common/FixoraIcons';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './AdminDashboardPage.module.css';

function formatVnd(amount: number): string {
  return `${Math.round(amount).toLocaleString('vi-VN')}đ`;
}

export default function AdminDashboardPage() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [slaAnalytics, setSlaAnalytics] = useState<SlaAnalytics | null>(null);
  const [profitReport, setProfitReport] = useState<ProfitReport | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchDashboardData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const [summaryRes, slaRes, profitRes] = await Promise.allSettled([
        reportsService.getDashboardSummary(),
        reportsService.getSlaAnalytics(),
        reportsService.getProfitReport({}),
      ]);

      if (summaryRes.status === 'fulfilled') {
        setSummary(summaryRes.value);
      } else {
        throw summaryRes.reason;
      }

      if (slaRes.status === 'fulfilled') {
        setSlaAnalytics(slaRes.value);
      }
      if (profitRes.status === 'fulfilled') {
        setProfitReport(profitRes.value);
      }
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchDashboardData();
  }, [fetchDashboardData]);

  if (isLoading) return <Loading />;
  if (errorMessage || !summary) {
    return (
      <ErrorMessage
        message={errorMessage ?? 'Không tải được dữ liệu quản trị.'}
        onRetry={() => void fetchDashboardData()}
      />
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>TRUNG TÂM ĐIỀU HÀNH QUẢN TRỊ — FIXORA</span>
          <h1 className={styles.title}>Dashboard Tổng quan &amp; Phân tích Chiến lược</h1>
          <p className={styles.subtitle}>
            Theo dõi toàn diện sức khỏe vận hành, doanh thu, biên lợi nhuận, cam kết SLA và hiệu suất đội ngũ trước khi đi sâu vào từng báo cáo.
          </p>
        </div>
        <div className={styles.shortcutRow}>
          <Button variant="secondary" size="sm" onClick={() => void fetchDashboardData()}>
            ↻ Làm mới dữ liệu
          </Button>
          <Button size="sm" onClick={() => navigate('/admin/tickets/create')}>
            + Tạo phiếu tiếp nhận
          </Button>
        </div>
      </div>

      {/* Hàng KPI Tổng quan Cấp cao */}
      <div className={styles.kpiRow}>
        <SummaryCard
          icon="📋"
          label="Tổng số phiếu sửa chữa"
          value={summary.repair.totalTickets}
          isEmphasized
        />
        <SummaryCard
          icon="💰"
          label="Doanh thu tháng này (đ)"
          value={summary.revenue.thisMonth}
        />
        <SummaryCard
          icon="📈"
          label="Lợi nhuận gộp lũy kế (đ)"
          value={profitReport?.grossProfit ?? 0}
        />
        <SummaryCard
          icon="👥"
          label="Tổng khách hàng hệ thống"
          value={summary.totalCustomers}
        />
      </div>

      {/* KHỐI TRỌNG TÂM: TỔNG QUAN CÁC TRANG PHÂN TÍCH QUAN TRỌNG TRƯỚC KHI XEM CHI TIẾT */}
      <div className={styles.sectionBlockHeader}>
        <div>
          <span className={styles.eyebrow}>BÁO CÁO &amp; PHÂN TÍCH CHUYÊN SÂU</span>
          <h2 className={styles.blockTitle}>Tổng quan các phân hệ phân tích trọng điểm</h2>
        </div>
        <span className={styles.blockHint}>
          Xem nhanh các chỉ số cốt lõi bên dưới và nhấn vào từng phân hệ để mở báo cáo chi tiết
        </span>
      </div>

      <div className={styles.analyticsHubGrid}>
        {/* 1. Phân tích Doanh thu */}
        <article
          className={styles.analyticsCard}
          onClick={() => navigate('/admin/reports/revenue')}
        >
          <div className={styles.analyticsCardTop}>
            <div className={styles.analyticsTitleWrap}>
              <span className={styles.analyticsIcon}>📊</span>
              <div>
                <span className={styles.analyticsKicker}>TÀI CHÍNH &amp; DÒNG TIỀN</span>
                <h3>Báo cáo Doanh thu</h3>
              </div>
            </div>
            <Badge variant="success">Đang tăng trưởng</Badge>
          </div>

          <p className={styles.analyticsDesc}>
            Biểu đồ xu hướng doanh thu theo ngày/tháng, tỷ lệ hóa đơn đã thanh toán và công nợ.
          </p>

          <div className={styles.metricMiniGrid}>
            <div className={styles.metricMiniBox}>
              <span>Hôm nay</span>
              <strong>{formatVnd(summary.revenue.today)}</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Tuần này</span>
              <strong>{formatVnd(summary.revenue.thisWeek)}</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Tháng này</span>
              <strong className={styles.highlightValue}>
                {formatVnd(summary.revenue.thisMonth)}
              </strong>
            </div>
          </div>

          <div className={styles.analyticsCardFooter}>
            <span>Mở phân tích doanh thu chi tiết</span>
            <span className={styles.arrowCue}>→</span>
          </div>
        </article>

        {/* 2. Phân tích Lợi nhuận (Profit Dashboard) */}
        <article
          className={styles.analyticsCard}
          onClick={() => navigate('/admin/reports/profit')}
        >
          <div className={styles.analyticsCardTop}>
            <div className={styles.analyticsTitleWrap}>
              <span className={styles.analyticsIcon}>💎</span>
              <div>
                <span className={styles.analyticsKicker}>HIỆU QUẢ KINH DOANH</span>
                <h3>Báo cáo Lợi nhuận (Profit)</h3>
              </div>
            </div>
            <Badge variant="info">
              Biên LN: {(profitReport?.marginPercent ?? 0).toFixed(1)}%
            </Badge>
          </div>

          <p className={styles.analyticsDesc}>
            Đối chiếu doanh thu dịch vụ với giá vốn linh kiện xuất kho và biên lợi nhuận theo dịch vụ.
          </p>

          <div className={styles.metricMiniGrid}>
            <div className={styles.metricMiniBox}>
              <span>Tổng doanh thu</span>
              <strong>{formatVnd(profitReport?.totalRevenue ?? summary.revenue.thisMonth)}</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Giá vốn linh kiện</span>
              <strong>{formatVnd(profitReport?.totalCost ?? 0)}</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Lợi nhuận gộp</span>
              <strong className={styles.highlightValue}>
                {formatVnd(profitReport?.grossProfit ?? 0)}
              </strong>
            </div>
          </div>

          <div className={styles.analyticsCardFooter}>
            <span>Xem Profit Dashboard &amp; Biên lợi nhuận</span>
            <span className={styles.arrowCue}>→</span>
          </div>
        </article>

        {/* 3. Giám sát SLA & Quá hạn */}
        <article
          className={styles.analyticsCard}
          onClick={() => navigate('/admin/reports/sla')}
        >
          <div className={styles.analyticsCardTop}>
            <div className={styles.analyticsTitleWrap}>
              <span className={styles.analyticsIcon}>⏱️</span>
              <div>
                <span className={styles.analyticsKicker}>CAM KẾT CHẤT LƯỢNG</span>
                <h3>Dashboard SLA &amp; Quá hạn</h3>
              </div>
            </div>
            <Badge
              variant={
                (slaAnalytics?.overdue ?? 0) > 0
                  ? 'danger'
                  : (slaAnalytics?.dueSoon ?? 0) > 0
                    ? 'warning'
                    : 'success'
              }
            >
              Tuân thủ: {slaAnalytics?.complianceRate ?? 100}%
            </Badge>
          </div>

          <p className={styles.analyticsDesc}>
            Phát hiện nút thắt cổ chai ở từng bước workflow, thiết bị quá hạn và tốc độ xử lý.
          </p>

          <div className={styles.metricMiniGrid}>
            <div className={styles.metricMiniBox}>
              <span>Đúng hạn</span>
              <strong className={styles.textSuccess}>{slaAnalytics?.onTrack ?? 0} phiếu</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Sắp trễ hạn</span>
              <strong className={styles.textWarning}>{slaAnalytics?.dueSoon ?? 0} phiếu</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Quá hạn SLA</span>
              <strong className={styles.textDanger}>{slaAnalytics?.overdue ?? 0} phiếu</strong>
            </div>
          </div>

          <div className={styles.analyticsCardFooter}>
            <span>Mở giám sát SLA &amp; Xử lý phiếu quá hạn</span>
            <span className={styles.arrowCue}>→</span>
          </div>
        </article>

        {/* 4. Kho linh kiện & Cảnh báo tồn kho */}
        <article
          className={styles.analyticsCard}
          onClick={() => navigate('/admin/inventory')}
        >
          <div className={styles.analyticsCardTop}>
            <div className={styles.analyticsTitleWrap}>
              <span className={styles.analyticsIcon}>📦</span>
              <div>
                <span className={styles.analyticsKicker}>CHUỖI CUNG ỨNG</span>
                <h3>Kho Linh kiện &amp; Tồn kho</h3>
              </div>
            </div>
            <Badge
              variant={
                summary.inventory.outOfStock > 0
                  ? 'danger'
                  : summary.inventory.lowStock > 0
                    ? 'warning'
                    : 'success'
              }
            >
              {summary.inventory.outOfStock > 0
                ? `${summary.inventory.outOfStock} mã hết hàng`
                : 'Tồn kho ổn định'}
            </Badge>
          </div>

          <p className={styles.analyticsDesc}>
            Kiểm soát số lượng tồn kho thực tế, định mức tối thiểu và lịch sử xuất nhập linh kiện.
          </p>

          <div className={styles.metricMiniGrid}>
            <div className={styles.metricMiniBox}>
              <span>Tổng danh mục</span>
              <strong>{summary.inventory.totalParts} mã</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Sắp hết hàng</span>
              <strong className={styles.textWarning}>{summary.inventory.lowStock} mã</strong>
            </div>
            <div className={styles.metricMiniBox}>
              <span>Đã hết hàng</span>
              <strong className={styles.textDanger}>{summary.inventory.outOfStock} mã</strong>
            </div>
          </div>

          <div className={styles.analyticsCardFooter}>
            <span>Quản lý kho &amp; Nhập xuất linh kiện</span>
            <span className={styles.arrowCue}>→</span>
          </div>
        </article>
      </div>

      {/* 2 CỘT: Phân bố trạng thái phiếu sửa chữa & Hiệu suất Kỹ thuật viên */}
      <div className={styles.twoColSection}>
        <section className={styles.section}>
          <div className={styles.sectionHeader}>
            <div>
              <span className={styles.eyebrow}>TIẾN ĐỘ VẬN HÀNH</span>
              <h3>Phân bố trạng thái phiếu sửa chữa ({summary.repair.totalTickets})</h3>
            </div>
            <Button variant="ghost" size="sm" onClick={() => navigate('/admin/tickets')}>
              Danh sách phiếu →
            </Button>
          </div>
          <div className={styles.statusList}>
            {summary.statusBreakdown.map((s) => {
              const pct =
                summary.repair.totalTickets > 0
                  ? Math.round((s.count / summary.repair.totalTickets) * 100)
                  : 0;
              return (
                <div key={s.statusCode} className={styles.statusRow}>
                  <span className={styles.statusLabel}>{s.statusLabel}</span>
                  <div className={styles.statusBarTrack}>
                    <div
                      className={styles.statusBarFill}
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                  <span className={styles.statusCount}>
                    {s.count} <small>({pct}%)</small>
                  </span>
                </div>
              );
            })}
          </div>
        </section>

        <section className={styles.section}>
          <div className={styles.sectionHeader}>
            <div>
              <span className={styles.eyebrow}>ĐỘI NGŨ KỸ THUẬT</span>
              <h3>Hiệu suất Kỹ thuật viên</h3>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/admin/reports/technicians')}
            >
              Xem phân tích KTV →
            </Button>
          </div>
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Kỹ thuật viên</th>
                  <th>Hoàn thành</th>
                  <th>Đang xử lý</th>
                  <th>TG hoàn tất TB</th>
                </tr>
              </thead>
              <tbody>
                {summary.technicians.slice(0, 6).map((t) => (
                  <tr key={t.technicianName}>
                    <td>
                      <strong>{t.technicianName}</strong>
                    </td>
                    <td>
                      <Badge variant="success">{t.completed} phiếu</Badge>
                    </td>
                    <td>
                      <Badge variant="info">{t.inProgress} phiếu</Badge>
                    </td>
                    <td>
                      {t.averageCompletionHours != null
                        ? `${t.averageCompletionHours.toFixed(1)} giờ`
                        : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>

      {/* Lối tắt Quản trị Danh mục & Cấu hình */}
      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <div>
            <span className={styles.eyebrow}>ĐIỀU HƯỚNG NHANH</span>
            <h3>Quản trị Danh mục, Nội dung &amp; Cấu hình Fixora</h3>
          </div>
        </div>
        <div className={styles.quickCatalogGrid}>
          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/services')}
          >
            <div className={styles.shortcutIconBox}>
              <Wrench size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Dịch vụ sửa chữa</strong>
              <span className={styles.shortcutSub}>Bảng giá, hình ảnh &amp; hiển thị</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/brands')}
          >
            <div className={styles.shortcutIconBox}>
              <Smartphone size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Hãng &amp; Dòng máy</strong>
              <span className={styles.shortcutSub}>Brand &amp; Device Model catalog</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/warranty-packages')}
          >
            <div className={styles.shortcutIconBox}>
              <ShieldCheck size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Gói bảo hành</strong>
              <span className={styles.shortcutSub}>Thời hạn &amp; điều khoản hậu mãi</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/diagnosis-symptoms')}
          >
            <div className={styles.shortcutIconBox}>
              <FileSearch size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Triệu chứng chẩn đoán</strong>
              <span className={styles.shortcutSub}>Gợi ý lỗi nhanh cho kỹ thuật viên</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/articles')}
          >
            <div className={styles.shortcutIconBox}>
              <BookOpen size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Bài viết &amp; Tin tức</strong>
              <span className={styles.shortcutSub}>Cẩm nang công nghệ website</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/reviews')}
          >
            <div className={styles.shortcutIconBox}>
              <Star size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Đánh giá khách hàng</strong>
              <span className={styles.shortcutSub}>Kiểm duyệt phản hồi dịch vụ</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/sla-policies')}
          >
            <div className={styles.shortcutIconBox}>
              <Clock size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Cấu hình SLA</strong>
              <span className={styles.shortcutSub}>Định mức thời gian từng bước</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>

          <button
            type="button"
            className={styles.catalogShortcut}
            onClick={() => navigate('/admin/users')}
          >
            <div className={styles.shortcutIconBox}>
              <User size={20} />
            </div>
            <div className={styles.shortcutContent}>
              <strong className={styles.shortcutTitle}>Nhân sự &amp; Phân quyền</strong>
              <span className={styles.shortcutSub}>Tài khoản Lễ tân, KTV, Admin</span>
            </div>
            <ArrowRight size={16} className={styles.shortcutArrow} />
          </button>
        </div>
      </section>
    </div>
  );
}
