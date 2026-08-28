// Khớp Backend.Application/Modules/Identity/DTOs (Tuần 3)
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresIn: number;
  role: string;
  email: string;
}

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  role: string;
  isActive: boolean;
}

export type UserRole = "Admin" | "Receptionist" | "Technician" | "Customer";
