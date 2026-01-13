import { create } from 'zustand';
import { dashboardService } from '../api/services/dashboardService';
import { MobileDashboardDto } from '../api/types/dashboard.types';

interface DashboardState {
  dashboard: MobileDashboardDto | null;
  isLoading: boolean;
  error: string | null;
  lastUpdate: Date | null;

  getDashboard: () => Promise<MobileDashboardDto>;
  getDeviceComponentData: (deviceId: string, componentId: string) => Promise<any>;
  refreshDashboard: () => Promise<void>;
  clearError: () => void;
}

export const useDashboardStore = create<DashboardState>((set, get) => ({
  dashboard: null,
  isLoading: false,
  error: null,
  lastUpdate: null,

  getDashboard: async () => {
    set({ isLoading: true, error: null });
    try {
      const dashboard = await dashboardService.getDashboard();
      set({
        dashboard,
        isLoading: false,
        lastUpdate: new Date(),
      });
      return dashboard;
    } catch (error: any) {set({
        error: error.detail || error.message || 'Failed to fetch dashboard',
        isLoading: false,
      });
      throw error;
    }
  },

  getDeviceComponentData: async (deviceId: string, componentId: string) => {
    set({ isLoading: true, error: null });
    try {
      const data = await dashboardService.getDeviceComponentData(deviceId, componentId);
      set({ isLoading: false });
      return data;
    } catch (error: any) {set({
        error: error.detail || error.message || 'Failed to fetch component data',
        isLoading: false,
      });
      throw error;
    }
  },

  refreshDashboard: async () => {
    await get().getDashboard();
  },

  clearError: () => {
    set({ error: null });
  },
}));

