import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, TrendingUp } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING, TYPOGRAPHY } from '../../../theme/constants';

export default function ConsumptionForecastScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: COLORS.dark }}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
        >
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.headerTitle, { color: COLORS.text }]}>
          {t('ai.consumptionForecast', 'Consumption Forecast')}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      {/* Empty State Content */}
      <View style={styles.content}>
        <View
          style={[
            styles.iconContainer,
            { backgroundColor: isDark ? 'rgba(16, 185, 129, 0.15)' : 'rgba(16, 185, 129, 0.1)' },
          ]}
        >
          <TrendingUp size={48} color="#10B981" strokeWidth={1.5} />
        </View>
        <Text style={[styles.title, { color: COLORS.text }]}>
          {t('ai.consumptionForecast', 'Consumption Forecast')}
        </Text>
        <Text style={[styles.description, { color: COLORS.textSoft }]}>
          {t('ai.consumptionForecastDetailDesc', 'AI predictions for your energy consumption patterns based on historical data and habits.')}
        </Text>
        <View style={[styles.badge, { backgroundColor: COLORS.accent + '20' }]}>
          <Text style={[styles.badgeText, { color: COLORS.accent }]}>
            {t('ai.comingSoon', 'Coming soon...')}
          </Text>
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: SPACING.md,
    paddingVertical: SPACING.sm,
  },
  backButton: {
    padding: SPACING.xs,
  },
  headerTitle: {
    fontSize: TYPOGRAPHY.lg,
    fontWeight: '600',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: SPACING.xl,
  },
  iconContainer: {
    width: 96,
    height: 96,
    borderRadius: 48,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: SPACING.lg,
  },
  title: {
    fontSize: TYPOGRAPHY.xl,
    fontWeight: '700',
    marginBottom: SPACING.sm,
    textAlign: 'center',
  },
  description: {
    fontSize: TYPOGRAPHY.sm,
    textAlign: 'center',
    lineHeight: 22,
    marginBottom: SPACING.lg,
  },
  badge: {
    paddingHorizontal: SPACING.md,
    paddingVertical: SPACING.xs,
    borderRadius: 20,
  },
  badgeText: {
    fontSize: TYPOGRAPHY.sm,
    fontWeight: '500',
  },
});
