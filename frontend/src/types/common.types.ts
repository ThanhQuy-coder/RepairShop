// Khớp Backend.Shared/Models/ApiErrorResponse.cs (Tuần 3)
export interface ApiErrorResponse {
  success: boolean;
  message: string;
  errors: string[];
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
}
