import { create } from 'zustand';

export type ThemeMode = 'light' | 'dark';

type ThemeState = {
  mode: ThemeMode;
  setMode: (m: ThemeMode) => void;
  toggle: () => void;
};

export const useThemeStore = create<ThemeState>((set, get) => ({
  mode: 'light',
  setMode: (mode) => set({ mode }),
  toggle: () => set({ mode: get().mode === 'dark' ? 'light' : 'dark' }),
}));
