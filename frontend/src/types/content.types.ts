export interface ServiceItem {
  id: string;
  name: string;
  description: string | null;
  basePrice: number | null;
  deviceType: string | null;
  isActive: boolean;
  imageUrl?: string | null;
  averageRating?: number;
  reviewCount?: number;
}

export interface ArticleListItem {
  id: string;
  title: string;
  imageUrl: string | null;
  createdAt: string;
}

export interface ArticleDetail extends ArticleListItem {
  content: string;
  isPublished: boolean;
}
