export interface RepairSummary {
  totalTickets: number;
  pending: number;
  inRepair: number;
  completed: number;
  cancelled: number;
}
export interface RevenueSummary {
  today: number;
  thisWeek: number;
  thisMonth: number;
}
export interface TechnicianSummaryItem {
  technicianName: string;
  completed: number;
  inProgress: number;
  averageCompletionHours: number | null;
}
export interface InventorySummary {
  totalParts: number;
  lowStock: number;
  outOfStock: number;
}

export interface DashboardSummary {
  repair: RepairSummary;
  revenue: RevenueSummary;
  technicians: TechnicianSummaryItem[];
  inventory: InventorySummary;
}

export interface RevenuePeriodItem {
  period: string;
  totalRevenue: number;
  ticketCount: number;
}
export interface RevenueReport {
  items: RevenuePeriodItem[];
  totalRevenue: number;
  totalInvoices: number;
  paidInvoices: number;
  unpaidInvoices: number;
}

export interface ProfitReportItem {
  ticketCode: string;
  revenue: number;
  cost: number;
  grossProfit: number;
  marginPercent: number;
}

export interface ProfitByServiceItem {
  serviceName: string;
  ticketCount: number;
  revenue: number;
  cost: number;
  grossProfit: number;
  marginPercent: number;
}

export interface ProfitReport {
  items: ProfitReportItem[];
  totalRevenue: number;
  totalCost: number;
  grossProfit: number;
  marginPercent: number;
  byService: ProfitByServiceItem[];
}

export interface StatusBreakdownItem {
  statusCode: string;
  statusLabel: string;
  count: number;
}

export interface SlaSummary {
  onTrack: number;
  dueSoon: number;
  overdue: number;
  activeTotal: number;
}

export type SlaStatus = 'OnTrack' | 'DueSoon' | 'Overdue' | 'Completed' | string;

export interface SlaTicketItem {
  ticketId: string;
  ticketCode: string;
  customerName: string;
  deviceLabel: string;
  statusCode: string;
  statusLabel: string;
  technicianName: string | null;
  startedAt: string;
  dueAt: string;
  slaStatus: SlaStatus | number;
  clockRunning: boolean;
}

export interface SlaTicketListResponse {
  items: SlaTicketItem[];
  total: number;
}

export interface SlaStepSummary {
  statusCode: string;
  label: string;
  total: number;
  overdue: number;
}

export interface SlaTechnicianSummary {
  technicianName: string;
  inProgress: number;
  dueSoon: number;
  overdue: number;
  onTimeRate: number;
}

export interface SlaDeviceSummary {
  deviceType: string;
  total: number;
  overdue: number;
}

export interface SlaAnalytics {
  total: number;
  onTrack: number;
  dueSoon: number;
  overdue: number;
  complianceRate: number;
  byStatusStep: SlaStepSummary[];
  byTechnician: SlaTechnicianSummary[];
  byDeviceType: SlaDeviceSummary[];
}

export interface SlaCompletedItem {
  ticket: SlaTicketItem;
  completedOnTime: boolean;
}

export interface SlaMyTicketsResponse {
  overdue: number;
  dueSoon: number;
  onTrack: number;
  items: SlaTicketItem[];
  recentCompleted: SlaCompletedItem[];
}

export interface DashboardSummary {
  repair: RepairSummary;
  revenue: RevenueSummary;
  technicians: TechnicianSummaryItem[];
  inventory: InventorySummary;
  statusBreakdown: StatusBreakdownItem[];
  totalCustomers: number;
}
