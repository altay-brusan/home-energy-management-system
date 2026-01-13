import { create } from 'zustand';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastState {
  visible: boolean;
  message: string;
  type: ToastType;
  title?: string;
  duration: number;
  show: (options: {
    message: string;
    type?: ToastType;
    title?: string;
    duration?: number;
  }) => void;
  hide: () => void;
}

export const useToastStore = create<ToastState>((set) => ({
  visible: false,
  message: '',
  type: 'info',
  title: undefined,
  duration: 4000,
  show: ({ message, type = 'info', title, duration = 4000 }) =>
    set({ visible: true, message, type, title, duration }),
  hide: () => set({ visible: false }),
}));
