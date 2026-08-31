import apiClient from './apiClient';
import type { Part } from '../types/inventory.types';

export const partService = {
  list: (search?: string) =>
    apiClient.get<Part[]>('/parts', { params: { search } }).then((res) => res.data),
};
