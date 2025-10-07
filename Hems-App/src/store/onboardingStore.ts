import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

export type Address = {
  line1: string;
  line2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
};

export type Home = {
  id: string;
  name: string;
  address: Address;
};

export type Device = {
  id: string;
  name: string;
  serialNumber: string;
  homeId: string;
};

type OnboardingState = {
  home: Home | null;
  device: Device | null;
  hasOnboarded: boolean;
  setHome: (input: { name: string; address: Address }) => void;
  setDevice: (input: { name: string; serialNumber: string }) => void;
  completeOnboarding: () => void;
  resetOnboarding: () => void;
};

export const useOnboardingStore = create<OnboardingState>()(
  persist(
    (set, get) => ({
      home: null,
      device: null,
      hasOnboarded: false,
      setHome: ({ name, address }) => {
        const homeId = `home-${Date.now()}`;
        set({
          home: {
            id: homeId,
            name: name.trim(),
            address: {
              line1: address.line1.trim(),
              line2: address.line2?.trim() || undefined,
              city: address.city.trim(),
              state: address.state?.trim() || undefined,
              postalCode: address.postalCode.trim(),
              country: address.country.trim(),
            },
          },
          device: null,
          hasOnboarded: false,
        });
      },
      setDevice: ({ name, serialNumber }) => {
        const home = get().home;
        if (!home) {
          throw new Error('Home must be defined before adding a device.');
        }

        set({
          device: {
            id: `device-${Date.now()}`,
            name: name.trim(),
            serialNumber: serialNumber.trim().toUpperCase(),
            homeId: home.id,
          },
        });
      },
      completeOnboarding: () => set({ hasOnboarded: true }),
      resetOnboarding: () => set({ home: null, device: null, hasOnboarded: false }),
    }),
    {
      name: 'onboarding-store',
      storage: createJSONStorage(() => AsyncStorage),
      version: 1,
    }
  )
);
