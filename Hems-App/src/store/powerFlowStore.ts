import { create } from 'zustand';

interface PowerFlowState {
  batteryPowerKw: number;
  setBatteryPowerKw: (power: number) => void;
}

export const usePowerFlowStore = create<PowerFlowState>((set) => ({
  batteryPowerKw: 0,
  setBatteryPowerKw: (power: number) => set({ batteryPowerKw: power }),
}));
