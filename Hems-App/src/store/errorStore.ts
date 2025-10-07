import { create } from 'zustand';
import type { AppError } from '../lib/http/error';

type ErrorState = {
  current: AppError | null;
  show: (err: AppError) => void;
  clear: () => void;
};

export const useErrorStore = create<ErrorState>((set) => ({
  current: null,
  show: (err) => set({ current: err }),
  clear: () => set({ current: null }),
}));
