import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import type { UserData } from '../lib/storage/token';

export type AuthUser = UserData;

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // State management methods
  setUser: (user: AuthUser) => void;
  clearAuth: () => void;

  // Role checking utilities
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  hasAllRoles: (roles: string[]) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      // State management
      setUser: (user: AuthUser) => {
        set({ user, isAuthenticated: true });
      },

      clearAuth: () => {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          error: null,
        });
      },

      hasRole: (role: string) => {
        const { user } = get();
        return user?.roles?.includes(role) || false;
      },

      hasAnyRole: (roles: string[]) => {
        const { user } = get();
        return user?.roles?.some(userRole => roles.includes(userRole)) || false;
      },

      hasAllRoles: (roles: string[]) => {
        const { user } = get();
        return user?.roles ? roles.every(role => user.roles.includes(role)) : false;
      },
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
