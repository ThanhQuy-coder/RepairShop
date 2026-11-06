import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTicketSummary } from '../../hooks/useTicketSummary';
import { useAppointmentSummary } from '../../hooks/useAppointmentSummary';
import { useAuth } from '../../hooks/useAuth';
import { ticketService } from '../../services/ticketService';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import type { TicketListItem } from '../../types/ticket.types';
import { Loading, ErrorMessage, Button, Badge } from '../../components/common';
import TicketStatusBadge from '../../components/ticket/TicketStatusBadge';
import SummaryCard from '../../components/dashboard/SummaryCard';
import styles from './DashboardPage.module.css';

const APPOINTMENT_STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Arrived: 'Khách đã đến',
  Completed: 'Hoàn thành',
  Rejected: 'Đã từ chối',
  Cancelled: 'Đã hủy',
  NoShow: 'Vắng mặt',
  ConvertedToTicket: 'Đã tạo phiếu',
};

const APPOINTMENT_TYPE_LABELS: Record<string, string> = {
  Repair: 'Sửa chữa',
  Pickup: 'Nhận máy',
  WarrantyRevisit: 'Bảo hành',
};

export default function StaffDashboardPage() {
  const navigate = useNavigate();
  const { role } = useAuth();
  const { summary, isLoading, errorMessage } = useTicketSummary();
  const {
    summary: appointmentSummary,
    isLoading: isAppointmentLoading,
    errorMessage: appointmentErrorMessage,
  } = useAppointmentSummary();

  const [recentTickets, setRecentTickets] = useState<TicketListItem[]>([]);
  const [recentAppointments, setRecentAppointments] = useState<AppointmentResponse[]>([]);
  const [isLoadingLists, setIsLoadingLists] = useState(true);

  useEffect(() => {
    let active = true;
    setIsLoadingLists(true);
    Promise.allSettled([
      ticketService.list({ page: 1, pageSize: 6 }),
      appointmentService.getForStaff(),
    ])
      .then(([ticketsRes, apptRes]) => {
        if (!active) return;
        if (ticketsRes.status === 'fulfilled') {
          setRecentTickets(ticketsRes.value.items);
        }
        if (apptRes.status === 'fulfilled') {
          const sorted = [...apptRes.value].sort((a, b) => {
            const pA = a.status === 'Pending' ? 0 : a.status === 'Confirmed' ? 1 : 2;
            const pB = b.status === 'Pending' ? 0 : b.status === 'Confirmed' ? 1 : 2;
            if (pA !== pB) return pA - pB;
            return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          });
          setRecentAppointments(sorted.slice(0, 6));
        }
      })
      .finally(() => {
        if (active) setIsLoadingLists(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const unassignedOrPendingTickets = recentTickets.filter(
    (t) => t.status === 'CHECKED_IN' || t.status === 'WAITING_APPROVAL' || t.status === 'READY_FOR_PICKUP'
  ).length;
  const pendingAppointmentsCount = recentAppointments.filter((a) => a.status === 'Pending').length;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>BÀN LÀM VIỆC LỄ TÂN — FIXORA</span>
          <h1 className={styles.title}>Tổng quan điều phối &amp; Tiếp nhận</h1>
          <p className={styles.subtitle}>
            Tiếp nhận thiết bị, xác nhận lịch hẹn khách hàng, điều phối kỹ thuật viên và kiểm soát tiến độ trả máy.
          </p>
        </div>
        <div className={styles.headerActions}>
          {role === 'Receptionist' && (
            <Button variant="secondary" size="md" onClick={() => navigate('/staff/reports/sla')}>
              ⏱ Giám sát SLA
            </Button>
          )}
          <Button variant="secondary" size="md" onClick={() => navigate('/staff/appointments')}>
            📅 Lịch hẹn
          </Button>
          <Button size="md" onClick={() => navigate('/tickets/create')}>
            + Tiếp nhận thiết bị
          </Button>
        </div>
      </div>

      {/* Khối thông báo điều phối nhanh trong ca */}
      <div className={styles.highlightBanner}>
        <div className={styles.highlightItem}>
          <span className={styles.highlightDot} data-tone="primary" />
          <div>
            <strong>{summary?.total ?? 0} phiếu sửa chữa</strong>
            <span>Đang được quản lý trên hệ thống</span>
          </div>
        </div>
        <div className={styles.highlightItem}>
          <span className={styles.highlightDot} data-tone="warning" />
          <div>
            <strong>{pendingAppointmentsCount} lịch hẹn chờ duyệt</strong>
            <span>Cần lễ tân liên hệ xác nhận sớm</span>
          </div>
        </div>
        <div className={styles.highlightItem}>
          <span className={styles.highlightDot} data-tone="info" />
          <div>
            <strong>{unassignedOrPendingTickets} phiếu cần lễ tân tác nghiệp</strong>
            <span>Mới tiếp nhận / Chờ duyệt giá / Chờ trả máy</span>
          </div>
        </div>
      </div>

      {isLoading && <Loading />}
      {errorMessage && <ErrorMessage message={errorMessage} />}

      {summary && (
        <section className={styles.statsSection}>
          <div className={styles.sectionHeaderRow}>
            <h2 className={styles.sectionHeading}>Trạng thái phiếu sửa chữa</h2>
            <button
              type="button"
              className={styles.sectionLinkBtn}
              onClick={() => navigate('/tickets')}
            >
              Xem toàn bộ danh sách →
            </button>
          </div>
          <div className={styles.grid}>
            <SummaryCard icon="📋" label="Tổng số phiếu" value={summary.total} isEmphasized />
            {summary.groups.map((g) => (
              <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
            ))}
          </div>
        </section>
      )}

      <section className={styles.statsSection}>
        <div className={styles.sectionHeaderRow}>
          <h2 className={styles.sectionHeading}>Trạng thái phiếu lịch hẹn</h2>
          <button
            type="button"
            className={styles.sectionLinkBtn}
            onClick={() => navigate('/staff/appointments')}
          >
            Quản lý lịch hẹn →
          </button>
        </div>
        {isAppointmentLoading && <Loading />}
        {appointmentErrorMessage && <ErrorMessage message={appointmentErrorMessage} />}
        {appointmentSummary && (
          <div className={styles.grid}>
            <SummaryCard
              icon="📅"
              label="Tổng số lịch hẹn"
              value={appointmentSummary.total}
              isEmphasized
            />
            {appointmentSummary.groups.map((g) => (
              <SummaryCard key={g.key} icon={g.icon} label={g.label} value={g.count} />
            ))}
          </div>
        )}
      </section>

      {/* Bảng điều phối trực tiếp: Phiếu sửa chữa gần đây & Lịch hẹn ưu tiên */}
      <section className={styles.livePanelsGrid}>
        <div className={styles.livePanel}>
          <div className={styles.livePanelHeader}>
            <div>
              <span className={styles.panelKicker}>HOẠT ĐỘNG TIẾP NHẬN</span>
              <h3>Phiếu sửa chữa gần đây</h3>
            </div>
            <Button variant="ghost" size="sm" onClick={() => navigate('/tickets')}>
              Tất cả phiếu →
            </Button>
          </div>

          {isLoadingLists ? (
            <Loading />
          ) : recentTickets.length === 0 ? (
            <p className={styles.emptyText}>Chưa có phiếu sửa chữa nào gần đây.</p>
          ) : (
            <div className={styles.liveList}>
              {recentTickets.map((ticket) => (
                <div
                  key={ticket.id}
                  className={styles.liveRow}
                  onClick={() => navigate(`/tickets/${ticket.id}`)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') navigate(`/tickets/${ticket.id}`);
                  }}
                >
                  <div className={styles.liveRowMain}>
                    <div className={styles.liveRowTop}>
                      <strong className={styles.liveCode}>{ticket.ticketCode}</strong>
                      <span className={styles.liveDate}>
                        {new Date(ticket.receivedAt).toLocaleDateString('vi-VN')}
                      </span>
                    </div>
                    <div className={styles.liveTitle}>
                      {ticket.customerName} · <span>{ticket.deviceLabel}</span>
                    </div>
                    <div className={styles.liveSub}>
                      KTV: {ticket.technicianName ?? 'Chưa phân công'}
                    </div>
                  </div>
                  <div className={styles.liveRowSide}>
                    <TicketStatusBadge status={ticket.status} />
                    <span className={styles.openHint}>Chi tiết →</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className={styles.livePanel}>
          <div className={styles.livePanelHeader}>
            <div>
              <span className={styles.panelKicker}>LỊCH HẸN KHÁCH HÀNG</span>
              <h3>Lịch hẹn chờ xác nhận &amp; sắp tới</h3>
            </div>
            <Button variant="ghost" size="sm" onClick={() => navigate('/staff/appointments')}>
              Quản lý lịch hẹn →
            </Button>
          </div>

          {isLoadingLists ? (
            <Loading />
          ) : recentAppointments.length === 0 ? (
            <p className={styles.emptyText}>Chưa có lịch hẹn nào cần xử lý.</p>
          ) : (
            <div className={styles.liveList}>
              {recentAppointments.map((appt) => (
                <div
                  key={appt.id}
                  className={styles.liveRow}
                  onClick={() => navigate(`/staff/appointments/${appt.id}`)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ')
                      navigate(`/staff/appointments/${appt.id}`);
                  }}
                >
                  <div className={styles.liveRowMain}>
                    <div className={styles.liveRowTop}>
                      <strong className={styles.liveCode}>{appt.fullName}</strong>
                      <span className={styles.typePill}>
                        {APPOINTMENT_TYPE_LABELS[appt.type] || appt.type}
                      </span>
                    </div>
                    <div className={styles.liveTitle}>
                      SĐT: {appt.phone}
                    </div>
                    <div className={styles.liveSub}>
                      📅 {new Date(appt.appointmentDate).toLocaleDateString('vi-VN')} · 🕒{' '}
                      {appt.slotStart} - {appt.slotEnd}
                    </div>
                  </div>
                  <div className={styles.liveRowSide}>
                    <Badge
                      variant={
                        appt.status === 'Pending'
                          ? 'warning'
                          : appt.status === 'Confirmed' || appt.status === 'Arrived'
                            ? 'info'
                            : appt.status === 'Completed' || appt.status === 'ConvertedToTicket'
                              ? 'success'
                              : 'default'
                      }
                    >
                      {APPOINTMENT_STATUS_LABELS[appt.status] || appt.status}
                    </Badge>
                    <span className={styles.openHint}>Xử lý →</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

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

          <div className={styles.actionCard} onClick={() => navigate('/staff/appointments')}>
            <div className={styles.actionIcon}>📅</div>
            <div className={styles.actionContent}>
              <h4>Quản lý lịch hẹn</h4>
              <p>Xác nhận lịch đặt trước, check-in khách đến và chuyển thành phiếu sửa</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>

          <div className={styles.actionCard} onClick={() => navigate('/tickets/quotes')}>
            <div className={styles.actionIcon}>💰</div>
            <div className={styles.actionContent}>
              <h4>Báo giá sửa chữa</h4>
              <p>Theo dõi các báo giá chờ khách duyệt và lịch sử chi phí linh kiện</p>
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
              <p>Danh mục máy tính, điện thoại, tablet và số Serial/IMEI theo khách hàng</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>

          <div className={styles.actionCard} onClick={() => navigate('/staff/warranty-lookup')}>
            <div className={styles.actionIcon}>🛡️</div>
            <div className={styles.actionContent}>
              <h4>Tra cứu bảo hành</h4>
              <p>Kiểm tra nhanh hiệu lực phiếu bảo hành theo mã BH, SĐT hoặc Serial</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>

          {role === 'Receptionist' && (
            <div className={styles.actionCard} onClick={() => navigate('/staff/reports/sla')}>
              <div className={styles.actionIcon}>⏱️</div>
              <div className={styles.actionContent}>
                <h4>Giám sát SLA Lễ tân</h4>
                <p>Theo dõi các phiếu chờ phân công, chờ duyệt báo giá và chờ khách nhận</p>
              </div>
              <span className={styles.actionArrow}>→</span>
            </div>
          )}

          <div className={styles.actionCard} onClick={() => navigate('/staff/ai-advisory')}>
            <div className={styles.actionIcon}>🤖</div>
            <div className={styles.actionContent}>
              <h4>AI Tư vấn lỗi &amp; Báo giá</h4>
              <p>Gợi ý nguyên nhân hỏng hóc, gói dịch vụ và ước tính chi phí tức thì</p>
            </div>
            <span className={styles.actionArrow}>→</span>
          </div>
        </div>
      </div>
    </div>
  );
}
