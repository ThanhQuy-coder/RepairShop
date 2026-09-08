import apiClient from './apiClient';
import type { AIAdviceRequest, AIAdviceResponse } from '../types/aiAdvisory.types';

export const aiAdvisoryService = {
  getAdvice: (payload: AIAdviceRequest) =>
    apiClient.post<AIAdviceResponse>('/ai/advice', payload).then((res) => res.data),
};
