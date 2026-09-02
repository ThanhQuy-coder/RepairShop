import { useToastStore } from '../store/toastStore';

// Hook mỏng che giấu Zustand khỏi component — page chỉ gọi showSuccess()/showError()
export function useToast() {
  const addToast = useToastStore((s) => s.addToast);
  return {
    showSuccess: (message: string) => addToast('success', message),
    showError: (message: string) => addToast('error', message),
    showInfo: (message: string) => addToast('info', message),
  };
}
