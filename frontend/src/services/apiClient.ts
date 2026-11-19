import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { SESSION_FLAG_KEY, useAuthStore } from '../store/authStore';
import type { AuthResponse, UserRole } from '../types/auth.types';

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const PUBLIC_PATH_PREFIXES = [
  '/services',
  '/estimate',
  '/consult',
  '/articles',
  '/book',
  '/appointments/lookup',
  '/track',
  '/login',
  '/register',
  '/unauthorized',
];

function requiresAuthPath(pathname: string): boolean {
  if (pathname === '/') return false;
  return !PUBLIC_PATH_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`)
  );
}

function hasStoredSession(): boolean {
  if (useAuthStore.getState().accessToken) return true;
  try {
    return window.localStorage.getItem(SESSION_FLAG_KEY) === '1';
  } catch {
    return false;
  }
}

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshPromise: Promise<AuthResponse> | null = null;

export function refreshAccessToken(): Promise<AuthResponse> {
  if (!refreshPromise) {
    refreshPromise = apiClient
      .post<AuthResponse>('/auth/refresh-token')
      .then((response) => {
        const data = response.data;
        useAuthStore
          .getState()
          .login(data.accessToken, data.role as UserRole, data.email, data.userId);
        return data;
      })
      .catch((err) => {
        useAuthStore.getState().clearSession();
        throw err;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;
    const originalRequest = error.config as RetryableRequestConfig | undefined;
    const requestUrl = originalRequest?.url ?? '';
    const isAuthEndpoint = requestUrl.includes('/auth/');

    if (
      status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isAuthEndpoint &&
      hasStoredSession()
    ) {
      originalRequest._retry = true;
      return refreshAccessToken()
        .then((data) => {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
          return apiClient(originalRequest);
        })
        .catch((refreshError) => {
          useAuthStore.getState().clearSession();
          if (!window.location.pathname.startsWith('/login')) {
            window.location.href = '/login';
          }
          return Promise.reject(refreshError);
        });
    }

    switch (status) {
      case 401:
        if (requestUrl.includes('/auth/refresh-token')) {
          useAuthStore.getState().clearSession();
          if (
            requiresAuthPath(window.location.pathname) &&
            !window.location.pathname.startsWith('/login')
          ) {
            window.location.href = '/login';
          }
        } else if (!isAuthEndpoint) {
          useAuthStore.getState().clearSession();
          if (
            requiresAuthPath(window.location.pathname) &&
            !window.location.pathname.startsWith('/login')
          ) {
            window.location.href = '/login';
          }
        }
        break;

      case 403:
        if (!window.location.pathname.startsWith('/unauthorized')) {
          window.location.href = '/unauthorized';
        }
        break;

      case 404:
      case 500:
      default:
        break;
    }

    return Promise.reject(error);
  }
);

export default apiClient;
