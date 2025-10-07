import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { NativeModules, Platform } from 'react-native';

import en from './locales/en.json';
import tr from './locales/tr.json';

// Cihaz dilini algıla
const getDeviceLanguage = (): 'tr' | 'en' => {
  try {
    const locale =
      Platform.OS === 'ios'
        ? NativeModules.SettingsManager?.settings?.AppleLocale ||
          NativeModules.SettingsManager?.settings?.AppleLanguages?.[0]
        : NativeModules.I18nManager?.localeIdentifier;

    const languageCode = locale?.split(/[-_]/)[0]?.toLowerCase();
    return languageCode === 'tr' ? 'tr' : 'en';
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