import { create } from 'zustand';

export type AlertLevel = 'info' | 'warning' | 'critical';

type AlertPayload = {
  title: string;
  message: string;
  level?: AlertLevel;
  deviceName?: string;
  recommendation?: string;
  onDetails?: () => void;
  onSupport?: () => void;
};

type AlertState = {
  visible: boolean;
  title: string;
  message: string;
  level: AlertLevel;
  deviceName?: string;
  recommendation?: string;
  timestamp: number | null;
  onDetails?: () => void;
  onSupport?: () => void;
  show: (p: AlertPayload) => void;
  hide: () => void;
};

export const useAlertStore = create<AlertState>((set) => ({
  visible: false,
  title: '',
  message: '',
  level: 'info',
  deviceName: undefined,
  recommendation: undefined,
  timestamp: null,
  show: ({ title, message, level = 'info', deviceName, recommendation, onDetails, onSupport }) =>
    set({ visible: true, title, message, level, deviceName, recommendation, timestamp: Date.now(), onDetails, onSupport }),
  hide: () => set({ visible: false }),
}));
