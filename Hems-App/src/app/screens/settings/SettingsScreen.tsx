import React from 'react';
import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Settings, ChevronRight, DollarSign } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makeSettingsScreenStyles } from './SettingsScreen.styles';

export default function SettingsScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeSettingsScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>{t('settings.title')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* General Settings Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={() => navigation.navigate('GeneralSettings')}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.accent}15` }]}>
            <Settings size={22} color={COLORS.accent} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.generalSettings')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.generalSettingsDesc')}
            </Text>
          </View>
          <ChevronRight size={20} color={COLORS.textSoft} />
        </TouchableOpacity>

        {/* Pricing Settings Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={() => navigation.navigate('PricingSettings')}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.accent}15` }]}>
            <DollarSign size={22} color={COLORS.accent} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.pricingSettings')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.pricingSettingsDesc')}
            </Text>
          </View>
          <ChevronRight size={20} color={COLORS.textSoft} />
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}
