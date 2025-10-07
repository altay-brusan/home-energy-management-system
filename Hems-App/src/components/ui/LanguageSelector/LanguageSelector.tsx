import React, { useState, useCallback, useRef, useMemo } from 'react';
import {
  View,
  Text,
  Pressable,
  Modal,
  ScrollView,
  Dimensions,
  Animated,
  Easing,
} from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18nStore } from '../../../store/i18nStore';
import type { Language } from '../../../store/i18nStore';
import { makeLanguageSelectorStyles } from './LanguageSelector.styles';

interface LanguageSelectorProps {
  style?: any;
}

const LANGUAGES = [
  {
    id: 'tr' as Language,
    label: 'Türkçe',
    nativeLabel: 'Türkçe',
    emoji: '🇹🇷',
    description: 'Türkiye',
  },
  {
    id: 'en' as Language,
    label: 'English',
    nativeLabel: 'English',
    emoji: '🇺🇸',
    description: 'United States',
  },
];

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

export default function LanguageSelector({ style }: LanguageSelectorProps) {
  const [modalVisible, setModalVisible] = useState(false);
  const { language, isLoading, setLanguage } = useI18nStore();
  
  const backdropAnim = useRef(new Animated.Value(0)).current;
  const translateYAnim = useRef(new Animated.Value(SCREEN_HEIGHT)).current;

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const COLORS_WITH_REQUIRED = {
    ...COLORS,
    primary: COLORS.accent, 
    text: COLORS.textSoft, 
    textSecondary: COLORS.slate400, 
  };
  const styles = useMemo(
    () => makeLanguageSelectorStyles(COLORS_WITH_REQUIRED, isDark),
    [COLORS_WITH_REQUIRED, isDark],
  );

  const currentLanguage = LANGUAGES.find(lang => lang.id === language);
  const isTurkish = language === 'tr';

  const openModal = useCallback(() => {
    setModalVisible(true);
    Animated.parallel([
      Animated.timing(backdropAnim, {
        toValue: 1,
        duration: 240,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.spring(translateYAnim, {
        toValue: 0,
        useNativeDriver: true,
        speed: 18,
        bounciness: 6,
      }),
    ]).start();
  }, [backdropAnim, translateYAnim]);

  const closeModal = useCallback(() => {
    Animated.parallel([
      Animated.timing(backdropAnim, {
        toValue: 0,
        duration: 180,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.timing(translateYAnim, {
        toValue: SCREEN_HEIGHT,
        duration: 200,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
    ]).start(() => {
      setModalVisible(false);
    });
  }, [backdropAnim, translateYAnim]);

  const handleLanguageSelect = useCallback(async (selectedLanguage: Language) => {
    await setLanguage(selectedLanguage);
    closeModal();
  }, [setLanguage, closeModal]);

  const backdropStyle = {
    opacity: backdropAnim,
  };

  const sheetStyle = {
    transform: [{ translateY: translateYAnim }],
  };

  return (
    <>
      <Pressable
        style={[styles.buttonContainer, style]}
        onPress={openModal}
        disabled={isLoading}
        android_ripple={{ color: COLORS.accent + '20' }}
      >
        <View style={styles.button}>
          <View style={styles.buttonContent}>
            <Text style={styles.emoji}>
              {isTurkish ? '🇹🇷' : '🇺🇸'}
            </Text>
            <View style={styles.textContainer}>
              <Text style={styles.languageText}>
                {isTurkish ? 'TR' : 'EN'}
              </Text>
            </View>
            <View style={styles.chevron}>
              <Text style={styles.chevronText}>▼</Text>
            </View>
          </View>
        </View>
      </Pressable>

      <Modal
        visible={modalVisible}
        transparent
        animationType="none"
        statusBarTranslucent
      >
        <View style={styles.modalContainer}>
          <Pressable style={styles.backdrop} onPress={closeModal}>
            <Animated.View style={[styles.backdropOverlay, backdropStyle]} />
          </Pressable>

          <Animated.View style={[styles.modalContent, sheetStyle]}>
            <View style={styles.sheet}>
              <View style={styles.handleContainer}>
                <View style={styles.handle} />
              </View>

              <View style={styles.header}>
                <Text style={styles.title}>Dil Seçin</Text>
                <Text style={styles.subtitle}>
                  {isTurkish ? 'Dil tercihinizi seçin' : 'Choose your language preference'}
                </Text>
              </View>

              <ScrollView
                style={styles.scrollView}
                showsVerticalScrollIndicator={false}
                bounces={false}
              >
                <View style={styles.languagesList}>
                  {LANGUAGES.map((lang) => (
                    <Pressable
                      key={lang.id}
                      style={[
                        styles.languageItem,
                        language === lang.id && styles.languageItemSelected,
                      ]}
                      onPress={() => handleLanguageSelect(lang.id)}
                    >
                      <View style={styles.languageItemContent}>
                        <View style={styles.languageEmoji}>
                          <Text style={styles.languageEmojiText}>{lang.emoji}</Text>
                        </View>
                        <View style={styles.languageInfo}>
                          <View style={styles.languageHeader}>
                            <Text
                              style={[
                                styles.languageLabel,
                                language === lang.id && styles.languageLabelSelected,
                              ]}
                            >
                              {lang.nativeLabel}
                            </Text>
                            {language === lang.id && (
                              <View style={styles.selectedIndicator}>
                                <Text style={styles.selectedIcon}>✓</Text>
                              </View>
                            )}
                          </View>
                          <Text
                            style={[
                              styles.languageDescription,
                              language === lang.id && styles.languageDescriptionSelected,
                            ]}
                          >
                            {lang.description}
                          </Text>
                        </View>
                      </View>
                    </Pressable>
                  ))}
                </View>
              </ScrollView>
            </View>
          </Animated.View>
        </View>
      </Modal>
    </>
  );
}
