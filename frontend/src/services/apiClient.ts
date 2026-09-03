import axios from 'axios';
import { useAuthStore } from '../store/authStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;

    switch (status) {
      case 401:
        // Token hết hạn / không hợp lệ -> logout ngay, không chờ page tự xử lý
        useAuthStore.getState().logout();
        if (!window.location.pathname.startsWith('/login')) {
          window.location.href = '/login';
        }
        break;

      case 403:
        // Có token hợp lệ nhưng không đủ quyền -> Forbidden Page (khác 401)
        if (!window.location.pathname.startsWith('/unauthorized')) {
          window.location.href = '/unauthorized';
        }
        break;

      case 404:
      case 500:
      default:
        // KHÔNG tự điều hướng — để page tự quyết định hiển thị (VD: "không tìm thấy",
        // hoặc "Đã xảy ra lỗi hệ thống") qua extractApiError() + <ErrorMessage />.
        // Điều hướng cứng ở đây sẽ phá luồng của những nơi CHỦ ĐỘNG kỳ vọng 404
        // (VD: kiểm tra trùng SĐT khách hàng) — đẩy quyết định UI về đúng nơi có ngữ cảnh.
        break;
    }

    return Promise.reject(error);
  }
);

export default apiClient;
