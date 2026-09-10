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
