import { create } from 'zustand';
import type { AppError } from '../lib/http/error';

type ErrorState = {
  current: AppError | null;
  criticalError: AppError | null; // Sunucu/ağ hatası gibi kritik hatalar için
  show: (err: AppError) => void;
  showCritical: (err: AppError) => void;
  clear: () => void;
  clearCritical: () => void;
};

// Kritik hata kontrolü - sunucu hatası veya ağ hatası
const isCriticalError = (err: AppError): boolean => {
  return (
    err.code === 'SERVER' ||
    err.code === 'NETWORK' ||
    (err.status !== undefined && err.status >= 500)
  );
};

export const useErrorStore = create<ErrorState>((set) => ({
  current: null,
  criticalError: null,
  show: (err) => {
    // Kritik hataları ayrı yere kaydet
    if (isCriticalError(err)) {
      set({ criticalError: err, current: null });
    } else {
      set({ current: err });
    }
  },
  showCritical: (err) => set({ criticalError: err, current: null }),
  clear: () => set({ current: null }),
  clearCritical: () => set({ criticalError: null }),
}));
