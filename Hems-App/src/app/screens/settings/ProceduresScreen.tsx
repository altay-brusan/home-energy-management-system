import React from 'react';
import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Battery, ChevronRight } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makeSettingsScreenStyles } from './SettingsScreen.styles';

export default function ProceduresScreen({ navigation }: any) {
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
        <Text style={[styles.title, { color: COLORS.text }]}>{t('settings.procedures')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Battery Recovery Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={() => navigation.navigate('BatteryRecovery')}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.accent}15` }]}>
            <Battery size={22} color={COLORS.accent} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.batteryRecovery')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.batteryRecoveryDesc')}
            </Text>
          </View>
          <ChevronRight size={20} color={COLORS.textSoft} />
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}
