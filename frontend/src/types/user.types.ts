import type { UserRole } from './auth.types';

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface UserProfileResponse {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  isActive: boolean;
}
