import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { NativeModules, Platform } from 'react-native';

import en from './locales/en.json';
import tr from './locales/tr.json';
import nl from './locales/nl.json';

// Cihaz dilini algıla
const getDeviceLanguage = (): 'tr' | 'en' | 'nl' => {
  try {
    const locale =
      Platform.OS === 'ios'
        ? NativeModules.SettingsManager?.settings?.AppleLocale ||
          NativeModules.SettingsManager?.settings?.AppleLanguages?.[0]
        : NativeModules.I18nManager?.localeIdentifier;

    const languageCode = locale?.split(/[-_]/)[0]?.toLowerCase();
    if (languageCode === 'tr') return 'tr';
    if (languageCode === 'nl') return 'nl';
    return 'en';
  } catch (error) {
    return 'en';
  }
};

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: en,
      },
      tr: {
        translation: tr,
      },
      nl: {
        translation: nl,
      },
    },
    lng: getDeviceLanguage(), // Cihaz dilini otomatik algıla
    fallbackLng: 'en', // Fallback İngilizce
    debug: __DEV__,
    interpolation: {
      escapeValue: false,
    },
    react: {
      useSuspense: false,
    },
  });

export default i18n;