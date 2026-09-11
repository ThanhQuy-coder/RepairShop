export interface ReviewListItem {
  id: string;
  rating: number;
  comment: string | null;
  customerName: string;
  isVisible: boolean;
  createdAt: string;
}
