import { useTranslation } from 'react-i18next';
import { useI18nStore, changeLanguage as changeLanguageStore } from '../store/i18nStore';
import type { Language } from '../store/i18nStore';

export const useI18n = () => {
  const { t } = useTranslation();
  const {
    language,
    isLoading,
    isInitialized,
    setLanguage,
    getCurrentLanguage,
    isTurkish,
    isEnglish,
  } = useI18nStore();

  const changeLanguage = async (language: Language) => {
    try {
      await setLanguage(language);
    } catch (error) {}
  };

  return {
    t,
    language,
    isLoading,
    isInitialized,
    changeLanguage,
    getCurrentLanguage,
    isTurkish,
    isEnglish,
    currentLanguage: language,
  };
};

export const useTranslationOnly = () => {
  const { t } = useTranslation();
  return { t };
};

export const useLanguage = () => {
  const {
    language,
    isLoading,
    isInitialized,
    setLanguage,
    getCurrentLanguage,
    isTurkish,
    isEnglish,
  } = useI18nStore();

  const changeLanguage = async (language: Language) => {
    try {
      await setLanguage(language);
    } catch (error) {}
  };

  return {
    language,
    isLoading,
    isInitialized,
    changeLanguage,
    getCurrentLanguage,
    isTurkish,
    isEnglish,
    currentLanguage: language,
  };
};
