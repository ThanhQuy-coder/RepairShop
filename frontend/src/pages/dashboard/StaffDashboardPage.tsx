import { useNavigate } from 'react-router-dom';
import { useTicketSummary } from '../../hooks/useTicketSummary';
import { Loading, ErrorMessage, Button } from '../../components/common';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './DashboardPage.module.css';

export default function StaffDashboardPage() {
  const navigate = useNavigate();
  const { summary, isLoading, errorMessage } = useTicketSummary();

  return (
    <div>
      <div className={styles.header}>
        <h2>Tổng quan</h2>
        <Button onClick={() => navigate('/tickets/create')}>+ Tiếp nhận thiết bị</Button>
      </div>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}

      {summary && (
        <div className={styles.grid}>
          <SummaryCard icon="📋" label="Tổng số phiếu" value={summary.total} isEmphasized />
          {summary.groups.map((g) => (
            <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
          ))}
        </div>
      )}

      <div className={styles.shortcuts}>
        <h3>Truy cập nhanh</h3>
        <div className={styles.shortcutRow}>
          <Button variant="secondary" onClick={() => navigate('/tickets')}>
            Danh sách phiếu sửa chữa
          </Button>
          <Button variant="secondary" onClick={() => navigate('/customers')}>
            Quản lý khách hàng
          </Button>
          <Button variant="secondary" onClick={() => navigate('/devices')}>
            Quản lý thiết bị
          </Button>
        </div>
      </div>
    </div>
  );
}
