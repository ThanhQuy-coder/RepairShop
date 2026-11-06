export interface ReviewListItem {
  id: string;
  rating: number;
  comment: string | null;
  customerName: string;
  isVisible: boolean;
  createdAt: string;
}

export interface ServiceReviewSummary {
  serviceId: string;
  averageRating: number;
  totalReviews: number;
  ratingDistribution: Record<string, number>;
}

export interface ServiceReviewItem {
  id: string;
  rating: number;
  comment: string | null;
  customerDisplayName: string;
  createdAt: string;
}

export interface ServiceReviewListResponse {
  summary: ServiceReviewSummary;
  items: ServiceReviewItem[];
  total: number;
  page: number;
  pageSize: number;
}
