// Khớp Backend.Application/Modules/Customers/DTOs (Tuần 3)
export interface Customer {
  id: string;
  fullName: string;
  phone: string;
  email?: string;
  address?: string;
  createdAt: string;
}

export interface CreateCustomerRequest {
  fullName: string;
  phone: string;
  email?: string;
  address?: string;
  userId?: string;
}

export interface UpdateCustomerRequest extends CreateCustomerRequest {
  id: string;
}
