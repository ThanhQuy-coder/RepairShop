import { useNavigate } from 'react-router-dom';
import { useTicketSummary } from '../../hooks/useTicketSummary';
import { Loading, ErrorMessage, Button } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './DashboardPage.module.css';

export default function StaffDashboardPage() {
  const navigate = useNavigate();
  const { summary, isLoading, errorMessage } = useTicketSummary();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>BÀN LÀM VIỆC LỄ TÂN</span>
          <h1 className={styles.title}>Tổng quan điều phối</h1>
          <p className={styles.subtitle}>Tiếp nhận thiết bị, theo dõi tiến độ và quản lý thông tin khách hàng.</p>
        </div>
        <Button size="lg" onClick={() => navigate('/tickets/create')}>
          + Tiếp nhận thiết bị
        </Button>
      </div>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}

      {summary && (
        <section className={styles.statsSection}>
          <h2 className={styles.sectionHeading}>Trạng thái phiếu sửa chữa</h2>
          <div className={styles.grid}>
            <SummaryCard icon="📋" label="Tổng số phiếu" value={summary.total} isEmphasized />
            {summary.groups.map((g) => (
              <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
            ))}
          </div>
        </section>
      )}

      <div className={styles.shortcuts}>
        <h2 className={styles.sectionHeading}>Truy cập nhanh nghiệp vụ</h2>
        <div className={styles.actionGrid}>
          <div className={styles.actionCard} onClick={() => navigate('/tickets')}>
            <div className={styles.actionIcon}>🎫</div>
            <div className={styles.actionContent}>
              <h4>Danh sách phiếu sửa chữa</h4>
              <p>Tra cứu tình trạng, bộ lọc kỹ thuật viên và phân công xử lý</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>

          <div className={styles.actionCard} onClick={() => navigate('/customers')}>
            <div className={styles.actionIcon}>👥</div>
            <div className={styles.actionContent}>
              <h4>Quản lý khách hàng</h4>
              <p>Hồ sơ khách hàng, số điện thoại liên hệ và lịch sử tiếp nhận</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>

          <div className={styles.actionCard} onClick={() => navigate('/devices')}>
            <div className={styles.actionIcon}>💻</div>
            <div className={styles.actionContent}>
              <h4>Quản lý thiết bị</h4>
              <p>Danh mục máy tính, điện thoại, tablet theo từng khách hàng</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>
        </div>
      </div>
    </div>
  );
}

