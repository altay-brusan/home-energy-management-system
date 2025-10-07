import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { NativeModules, Platform } from 'react-native';
import i18n from '../lib/i18n';

export type Language = 'tr' | 'en';

// Cihaz dilini algıla
const getDeviceLanguage = (): Language => {
  try {
    const locale =
      Platform.OS === 'ios'
        ? NativeModules.SettingsManager?.settings?.AppleLocale ||
          NativeModules.SettingsManager?.settings?.AppleLanguages?.[0]
        : NativeModules.I18nManager?.localeIdentifier;

    // Dili normalize et (tr-TR → tr, en-US → en)
    const languageCode = locale?.split(/[-_]/)[0]?.toLowerCase();

    // Türkçe ise 'tr', değilse 'en'
    return languageCode === 'tr' ? 'tr' : 'en';
  } catch (error) {
    console.error('Error detecting device language:', error);
    return 'en'; // Varsayılan İngilizce
  }
};

interface I18nState {
  language: Language;
  isLoading: boolean;
  isInitialized: boolean;
  setLanguage: (language: Language) => Promise<void>;
  initializeLanguage: () => Promise<void>;
  getCurrentLanguage: () => Language;
  isTurkish: () => boolean;
  isEnglish: () => boolean;
}

export const useI18nStore = create<I18nState>()(
  persist(
    (set, get) => ({
      language: getDeviceLanguage(), // Cihaz dilini otomatik algıla
      isLoading: false,
      isInitialized: false,

      setLanguage: async (language: Language) => {
        set({ isLoading: true });
        try {
          // i18next'e dil değişikliğini bildir
          await i18n.changeLanguage(language);
          
          // Store'u güncelle
          set({ language, isLoading: false });
        } catch (error) {
          console.error('Language change error:', error);
          set({ isLoading: false });
        }
      },

      initializeLanguage: async () => {
        if (get().isInitialized) return;
        
        set({ isLoading: true });
        try {
          // Zustand persist zaten dilı yükledi
          // Manuel AsyncStorage okumaya gerek yok
          const currentLanguage = get().language;
          
          // i18next'i mevcut dil ile senkronize et
          await i18n.changeLanguage(currentLanguage);
          
          set({ 
            isLoading: false, 
            isInitialized: true 
          });
        } catch (error) {
          console.error('Language initialization error:', error);
          set({ 
            isLoading: false, 
            isInitialized: true 
          });
        }
      },

      getCurrentLanguage: () => {
        return get().language;
      },

      isTurkish: () => {
        return get().language === 'tr';
      },

      isEnglish: () => {
        return get().language === 'en';
      },
    }),
    {
      name: 'i18n-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({ 
        language: state.language,
        isInitialized: state.isInitialized 
      }),
      onRehydrateStorage: () => (state) => {
        // Rehydration tamamlandığında i18next'i güncelle
        if (state?.language) {
          i18n.changeLanguage(state.language);
        }
      },
    }
  )
);

// Store'u başlatmak için helper fonksiyon
export const initializeI18nStore = async () => {
  const store = useI18nStore.getState();
  await store.initializeLanguage();
};

// Dil değiştirme helper fonksiyonu
export const changeLanguage = async (language: Language) => {
  const store = useI18nStore.getState();
  await store.setLanguage(language);
};

// Mevcut dil helper fonksiyonları
export const getCurrentLanguage = (): Language => {
  return useI18nStore.getState().getCurrentLanguage();
};

export const isTurkish = (): boolean => {
  return useI18nStore.getState().isTurkish();
};

export const isEnglish = (): boolean => {
  return useI18nStore.getState().isEnglish();
};