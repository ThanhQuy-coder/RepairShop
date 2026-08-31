import type { UserRole } from './auth.types';

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}
