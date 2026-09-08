export interface AISuggestedServiceItem {
  serviceId: string;
  serviceName: string;
  priceRangeMin: number;
  priceRangeMax: number;
}

export interface AIAdviceRequest {
  deviceType: string;
  brand: string;
  model: string;
  issueDescription: string;
}

export interface AIAdviceResponse {
  aiAvailable: boolean;
  suggestedServices: AISuggestedServiceItem[];
  suggestedParts: string[];
  explanation: string | null;
  message: string | null;
  disclaimer: string;
}