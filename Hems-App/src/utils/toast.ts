import { useToastStore } from '../store/toastStore';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastOptions {
  title?: string;
  message: string;
  type?: ToastType;
  duration?: number;
}

export const showToast = ({ title, message, type = 'info', duration = 4000 }: ToastOptions) => {
  const { show } = useToastStore.getState();
  show({
    message,
    type,
    title,
    duration,
  });
};

export const toast = {
  success: (message: string, title?: string) => {
    showToast({ message, title, type: 'success' });
  },

  error: (message: string, title?: string) => {
    showToast({ message, title, type: 'error' });
  },

  warning: (message: string, title?: string) => {
    showToast({ message, title, type: 'warning' });
  },

  info: (message: string, title?: string) => {
    showToast({ message, title, type: 'info' });
  },
};

