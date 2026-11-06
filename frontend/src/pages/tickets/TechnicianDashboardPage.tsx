import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ticketService } from '../../services/ticketService';
import { extractApiError } from '../../utils/apiError';
import type { TicketListItem } from '../../types/ticket.types';
import { Badge, Loading, ErrorMessage, EmptyState, Button } from '../../components/common';
import {
  TICKET_STATUS_LABELS,
  TICKET_STATUS_BADGE_VARIANT,
  STATUS_ICON,
} from '../../constants/ticketStatus';
import styles from './TechnicianDashboardPage.module.css';

const WORKFLOW_STAGES = [
  {
    code: 'ASSIGNED',
    step: 1,
    shortLabel: 'Mới phân công',
    hint: 'Nhận máy & bắt đầu kiểm tra',
    cta: 'Bắt đầu chẩn đoán →',
  },
  {
    code: 'DIAGNOSING',
    step: 2,
    shortLabel: 'Đang kiểm tra',
    hint: 'Xác định lỗi & lập phương án',
    cta: 'Cập nhật chẩn đoán →',
  },
  {
    code: 'WAITING_PARTS',
    step: 3,
    shortLabel: 'Chờ linh kiện',
    hint: 'Đợi xuất kho linh kiện thay thế',
    cta: 'Kiểm tra linh kiện →',
  },
  {
    code: 'IN_REPAIR',
    step: 4,
    shortLabel: 'Đang sửa chữa',
    hint: 'Thực hiện thay thế & khắc phục',
    cta: 'Cập nhật sửa chữa →',
  },
  {
    code: 'QA_TESTING',
    step: 5,
    shortLabel: 'Kiểm thử QA',
    hint: 'Test chức năng trước khi trả máy',
    cta: 'Thực hiện QA →',
  },
];

const GROUPS = WORKFLOW_STAGES.map((s) => s.code);

function getRelativeTime(dateStr: string): string {
  const diffMs = Date.now() - new Date(dateStr).getTime();
  const hours = Math.max(1, Math.floor(diffMs / (1000 * 60 * 60)));
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  return `${days} ngày trước`;
}

export default function TechnicianDashboardPage() {
  const navigate = useNavigate();
  const [tickets, setTickets] = useState<TicketListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [viewMode, setViewMode] = useState<'grouped' | 'kanban'>('grouped');
  const [hideEmptyGroups, setHideEmptyGroups] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const loadTickets = useCallback((showLoading = true) => {
    if (showLoading) setIsLoading(true);
    setIsRefreshing(true);
    setErrorMessage(null);
    ticketService
      .list({ pageSize: 200 })
      .then((res) => setTickets(res.items))
      .catch((err) => setErrorMessage(extractApiError(err).message))
      .finally(() => {
        setIsLoading(false);
        setIsRefreshing(false);
      });
  }, []);

  useEffect(() => {
    loadTickets();
  }, [loadTickets]);

  const filteredTickets = useMemo(() => {
    const query = normalize(search);
    return tickets.filter((ticket) => {
      const searchableText = [
        ticket.ticketCode,
        ticket.customerName,
        ticket.deviceLabel,
        ticket.issueReported,
        TICKET_STATUS_LABELS[ticket.status],
      ]
        .map(normalize)
        .join(' ');
      const matchesStatus = statusFilter === 'ALL' || ticket.status === statusFilter;
      const matchesSearch = !query || searchableText.includes(query);
      return matchesStatus && matchesSearch;
    });
  }, [tickets, search, statusFilter]);

  const isFiltering = Boolean(search.trim()) || statusFilter !== 'ALL';

  const visibleStages = useMemo(() => {
    return WORKFLOW_STAGES.filter((stage) => {
      if (statusFilter !== 'ALL' && stage.code !== statusFilter) return false;
      if (isFiltering || hideEmptyGroups) {
        return filteredTickets.some((t) => t.status === stage.code);
      }
      return true;
    });
  }, [statusFilter, isFiltering, hideEmptyGroups, filteredTickets]);

  const activeCount = tickets.filter((t) => GROUPS.includes(t.status)).length;
  const waitingApprovalCount = tickets.filter((t) => t.status === 'WAITING_APPROVAL').length;
  const completedCount = tickets.filter((t) =>
    ['READY_FOR_PICKUP', 'DELIVERED'].includes(t.status)
  ).length;

  if (isLoading) return <Loading />;
  if (errorMessage) return <ErrorMessage message={errorMessage} onRetry={() => loadTickets(true)} />;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>BÀN LÀM VIỆC KỸ THUẬT — FIXORA</span>
          <h1 className={styles.title}>Công việc của tôi ({activeCount} đang xử lý)</h1>
          <p className={styles.subtitle}>
            Quản lý trực quan quy trình 5 bước kỹ thuật: Tiếp nhận phân công → Chẩn đoán → Linh kiện → Sửa chữa → Kiểm thử QA.
          </p>
        </div>
        <div className={styles.headerActions}>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate('/technician/sla')}
          >
            ⏱ SLA của tôi
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => loadTickets(false)}
            isLoading={isRefreshing}
          >
            ↻ Làm mới
          </Button>
        </div>
      </div>

      {/* Thanh Pipeline 5 bước trực quan — bấm vào từng bước để lọc */}
      <div className={styles.pipelineStrip}>
        {WORKFLOW_STAGES.map((stage) => {
          const count = tickets.filter((t) => t.status === stage.code).length;
          const isSelected = statusFilter === stage.code;
          return (
            <button
              key={stage.code}
              type="button"
              className={`${styles.pipelineStep} ${isSelected ? styles.pipelineStepActive : ''}`}
              onClick={() => setStatusFilter(isSelected ? 'ALL' : stage.code)}
            >
              <div className={styles.pipelineStepTop}>
                <span className={styles.pipelineStepBadge}>
                  Bước {stage.step} · {STATUS_ICON[stage.code]}
                </span>
                <strong className={styles.pipelineStepCount}>{count}</strong>
              </div>
              <div className={styles.pipelineStepName}>{stage.shortLabel}</div>
              <div className={styles.pipelineStepHint}>{stage.hint}</div>
            </button>
          );
        })}
      </div>

      {/* Chỉ số phụ (Chờ khách duyệt giá & Đã bàn giao xong) */}
      <div className={styles.secondaryStatsBar}>
        <div className={styles.secondaryStatItem}>
          <span>🔧 Đang trong xưởng kỹ thuật:</span>
          <strong>{activeCount} phiếu</strong>
        </div>
        <div className={styles.secondaryStatItem}>
          <span>📝 Đang chờ khách xác nhận báo giá:</span>
          <strong>{waitingApprovalCount} phiếu</strong>
        </div>
        <div className={styles.secondaryStatItem}>
          <span>✅ Đã hoàn tất sửa chữa / Bàn giao:</span>
          <strong>{completedCount} phiếu</strong>
        </div>
      </div>

      {/* Thanh công cụ tìm kiếm + lọc + đổi chế độ xem */}
      <div className={styles.toolbar}>
        <label className={styles.searchBox}>
          <span aria-hidden="true">⌕</span>
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm mã phiếu, tên khách hàng, model máy hoặc mô tả lỗi..."
            aria-label="Tìm ticket"
          />
        </label>

        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
          aria-label="Lọc trạng thái"
        >
          <option value="ALL">Tất cả giai đoạn kỹ thuật ({activeCount})</option>
          {WORKFLOW_STAGES.map((stage) => (
            <option key={stage.code} value={stage.code}>
              Bước {stage.step}: {TICKET_STATUS_LABELS[stage.code]}
            </option>
          ))}
        </select>

        {viewMode === 'grouped' && (
          <label className={styles.toggleCheck}>
            <input
              type="checkbox"
              checked={hideEmptyGroups}
              onChange={(e) => setHideEmptyGroups(e.target.checked)}
            />
            <span>Ẩn nhóm trống</span>
          </label>
        )}

        <div className={styles.viewSwitcher}>
          <button
            type="button"
            className={`${styles.viewBtn} ${viewMode === 'grouped' ? styles.viewBtnActive : ''}`}
            onClick={() => setViewMode('grouped')}
          >
            ☰ Theo giai đoạn
          </button>
          <button
            type="button"
            className={`${styles.viewBtn} ${viewMode === 'kanban' ? styles.viewBtnActive : ''}`}
            onClick={() => setViewMode('kanban')}
          >
            ▦ Bảng Kanban
          </button>
        </div>

        {isFiltering && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearch('');
              setStatusFilter('ALL');
            }}
          >
            Xóa lọc
          </Button>
        )}
      </div>

      {isFiltering && (
        <p className={styles.resultSummary}>
          Đang hiển thị <strong>{filteredTickets.length}</strong> phiếu phù hợp với bộ lọc
        </p>
      )}

      {viewMode === 'kanban' ? (
        <div className={styles.kanbanBoard}>
          {WORKFLOW_STAGES.map((stage) => {
            const items = filteredTickets.filter((t) => t.status === stage.code);
            return (
              <div key={stage.code} className={styles.kanbanCol}>
                <div className={styles.kanbanColHeader}>
                  <div>
                    <span className={styles.kanbanStepNum}>BƯỚC {stage.step}</span>
                    <h3>{stage.shortLabel}</h3>
                  </div>
                  <span className={styles.countPill}>{items.length}</span>
                </div>
                <div className={styles.kanbanColBody}>
                  {items.length === 0 ? (
                    <div className={styles.emptyGroupBox}>Trống</div>
                  ) : (
                    items.map((t) => (
                      <TechTicketCard
                        key={t.id}
                        ticket={t}
                        stage={stage}
                        onOpen={() => navigate(`/tickets/${t.id}`)}
                      />
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className={styles.groupContainer}>
          {visibleStages.map((stage) => {
            const items = filteredTickets.filter((t) => t.status === stage.code);
            return (
              <section key={stage.code} className={styles.group}>
                <div className={styles.groupHeader}>
                  <div className={styles.groupTitleWrap}>
                    <span className={styles.stageStepPill}>Bước {stage.step}/5</span>
                    <Badge variant={TICKET_STATUS_BADGE_VARIANT[stage.code]}>
                      {STATUS_ICON[stage.code]} {TICKET_STATUS_LABELS[stage.code]}
                    </Badge>
                    <span className={styles.stageHintText}>{stage.hint}</span>
                  </div>
                  <span className={styles.countPill}>{items.length} phiếu</span>
                </div>

                {items.length === 0 ? (
                  <div className={styles.emptyGroupBox}>
                    <span>Không có phiếu nào đang ở bước "{TICKET_STATUS_LABELS[stage.code]}"</span>
                  </div>
                ) : (
                  <div className={styles.cardGrid}>
                    {items.map((t) => (
                      <TechTicketCard
                        key={t.id}
                        ticket={t}
                        stage={stage}
                        onOpen={() => navigate(`/tickets/${t.id}`)}
                      />
                    ))}
                  </div>
                )}
              </section>
            );
          })}
        </div>
      )}

      {tickets.length === 0 && <EmptyState message="Bạn chưa được phân công phiếu sửa chữa nào." />}
      {tickets.length > 0 && filteredTickets.length === 0 && (
        <EmptyState message="Không tìm thấy phiếu sửa chữa phù hợp với bộ lọc hiện tại." />
      )}
    </div>
  );
}

function TechTicketCard({
  ticket,
  stage,
  onOpen,
}: {
  ticket: TicketListItem;
  stage: (typeof WORKFLOW_STAGES)[number];
  onOpen: () => void;
}) {
  const progressPercent = Math.round((stage.step / 5) * 100);

  return (
    <div
      className={styles.card}
      onClick={onOpen}
      role="button"
      tabIndex={0}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') onOpen();
      }}
    >
      <div className={styles.cardHeader}>
        <strong className={styles.ticketCode}>{ticket.ticketCode}</strong>
        <span className={styles.receivedAt} title={new Date(ticket.receivedAt).toLocaleString('vi-VN')}>
          🕒 {getRelativeTime(ticket.receivedAt)}
        </span>
      </div>

      <div className={styles.deviceLine}>
        <span className={styles.deviceBadge}>💻 {ticket.deviceLabel}</span>
      </div>

      <div className={styles.customerLine}>
        <span className={styles.customerIcon}>👤</span>
        <span className={styles.customerName}>Khách: {ticket.customerName}</span>
      </div>

      <div className={styles.issueBox}>
        <span className={styles.issueLabel}>Mô tả lỗi ban đầu:</span>
        <p className={styles.issue}>
          {ticket.issueReported || 'Chưa có mô tả chi tiết — kiểm tra trực tiếp trên máy.'}
        </p>
      </div>

      <div className={styles.stepProgressWrap}>
        <div className={styles.stepProgressMeta}>
          <span>Tiến độ kỹ thuật: Bước {stage.step}/5</span>
          <strong>{progressPercent}%</strong>
        </div>
        <div className={styles.stepProgressTrack}>
          <div className={styles.stepProgressFill} style={{ width: `${progressPercent}%` }} />
        </div>
      </div>

      <div className={styles.cardFooter}>
        <span>{stage.cta}</span>
      </div>
    </div>
  );
}

function normalize(value: string | null | undefined) {
  return (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase()
    .trim();
}
