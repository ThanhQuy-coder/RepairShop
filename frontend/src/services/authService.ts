import apiClient from './apiClient';
import type { LoginRequest, RegisterRequest, AuthResponse } from '../types/auth.types';

export const authService = {
  login: (payload: LoginRequest) =>
    apiClient.post<AuthResponse>('/auth/login', payload).then((res) => res.data),

  register: (payload: RegisterRequest) =>
    apiClient.post<AuthResponse>('/auth/register', payload).then((res) => res.data),
};
