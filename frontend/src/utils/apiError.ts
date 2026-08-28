import { AxiosError } from 'axios';
import type { ApiErrorResponse } from '../types/common.types';

export function extractApiError(error: unknown): ApiErrorResponse {
  const axiosError = error as AxiosError<ApiErrorResponse>;
  if (axiosError.response?.data) {
    return axiosError.response.data;
  }
  return {
    success: false,
    message: 'Không thể kết nối tới máy chủ. Vui lòng thử lại.',
    errors: [],
  };
}
