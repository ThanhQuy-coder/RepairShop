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

export interface StatusBreakdownItem {
  statusCode: string;
  statusLabel: string;
  count: number;
}

export interface DashboardSummary {
  repair: RepairSummary;
  revenue: RevenueSummary;
  technicians: TechnicianSummaryItem[];
  inventory: InventorySummary;
  statusBreakdown: StatusBreakdownItem[];
  totalCustomers: number;
}
