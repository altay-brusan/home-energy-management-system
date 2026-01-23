import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  Platform,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import LinearGradient from 'react-native-linear-gradient';
import {
  Sun,
  Zap,
  TrendingUp,
  Wrench,
  X,
} from 'lucide-react-native';

const NetzrAiImage = require('../../../assets/images/ai/netzr-ai.jpeg');
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING, TYPOGRAPHY } from '../../../theme/constants';

type TabType = 'forecast' | 'maintenance';

interface TabButtonProps {
  label: string;
  isActive: boolean;
  onPress: () => void;
  colors: Record<string, string>;
}

const TabButton: React.FC<TabButtonProps> = ({ label, isActive, onPress, colors }) => {
  return (
    <TouchableOpacity onPress={onPress} style={styles.tabButton}>
      <Text
        style={[
          styles.tabText,
          {
            color: isActive ? colors.text : '#9CA3AF',
            fontWeight: '700',
          },
        ]}
      >
        {label}
      </Text>
    </TouchableOpacity>
  );
};

interface HeroBannerProps {
  onClose?: () => void;
}

const HeroBanner: React.FC<HeroBannerProps> = ({ onClose }) => {
  return (
    <View style={styles.heroBanner}>
      {/* Background Image */}
      <Image
        source={NetzrAiImage}
        style={styles.heroImage}
        resizeMode="cover"
      />

      {/* Close Button */}
      {onClose && (
        <TouchableOpacity style={styles.closeButton} onPress={onClose}>
          <X size={20} color="#6B7280" />
        </TouchableOpacity>
      )}
    </View>
  );
};

interface FeatureCardProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  iconBgColor: string;
  onPress: () => void;
  colors: Record<string, string>;
  isDark: boolean;
}

const FeatureCard: React.FC<FeatureCardProps> = ({
  title,
  description,
  icon,
  iconBgColor,
  onPress,
  colors,
  isDark,
}) => {
  return (
    <TouchableOpacity
      onPress={onPress}
      activeOpacity={0.7}
      style={[
        styles.featureCard,
        {
          backgroundColor: isDark ? colors.surface : '#FFFFFF',
          borderWidth: isDark ? 0 : 1,
          borderColor: isDark ? 'transparent' : 'rgba(0,0,0,0.04)',
        },
      ]}
    >
      <View style={styles.featureCardContent}>
        <Text style={[styles.featureCardTitle, { color: colors.text }]}>
          {title}
        </Text>
        <Text style={[styles.featureCardDescription, { color: colors.textSoft }]}>
          {description}
        </Text>
      </View>
      <View
        style={[
          styles.featureCardVisual,
          { backgroundColor: iconBgColor + '15' },
        ]}
      >
        {icon}
      </View>
    </TouchableOpacity>
  );
};

const MaintenanceEmptyState: React.FC<{ colors: Record<string, string> }> = ({ colors }) => {
  const { t } = useTranslation();

  return (
    <View style={styles.emptyState}>
      <View
        style={[
          styles.emptyStateIcon,
          { backgroundColor: colors.bgShadeLight },
        ]}
      >
        <Wrench size={40} color={colors.textSoft} strokeWidth={1.5} />
      </View>
      <Text style={[styles.emptyStateTitle, { color: colors.text }]}>
        {t('ai.maintenanceTitle', 'Maintenance')}
      </Text>
      <Text style={[styles.emptyStateDescription, { color: colors.textSoft }]}>
        {t('ai.comingSoon', 'Coming soon...')}
      </Text>
    </View>
  );
};

export default function AIScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<any>();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const [activeTab, setActiveTab] = useState<TabType>('forecast');
  const [showBanner, setShowBanner] = useState(true);

  const forecastCards = [
    {
      id: 'solar',
      icon: <Sun size={32} color="#F59E0B" strokeWidth={1.5} />,
      iconBgColor: '#F59E0B',
      title: t('ai.solarForecast', 'Solar Forecast'),
      description: t('ai.solarForecastDesc', 'Predicted solar energy generation'),
      screen: 'SolarForecast',
    },
    {
      id: 'blackout',
      icon: <Zap size={32} color="#EF4444" strokeWidth={1.5} />,
      iconBgColor: '#EF4444',
      title: t('ai.blackoutScenario', 'Blackout Scenario'),
      description: t('ai.blackoutScenarioDesc', 'Battery backup analysis'),
      screen: 'BlackoutScenario',
    },
    {
      id: 'consumption',
      icon: <TrendingUp size={32} color="#10B981" strokeWidth={1.5} />,
      iconBgColor: '#10B981',
      title: t('ai.consumptionForecast', 'Consumption Forecast'),
      description: t('ai.consumptionForecastDesc', 'Energy usage predictions'),
      screen: 'ConsumptionForecast',
    },
  ];

  return (
    <SafeAreaView
      style={[
        styles.container,
        { backgroundColor: isDark ? 'rgba(28, 34, 43, 1)' : 'rgba(243, 243, 245, 1)' },
      ]}
    >
      {/* Gradient Background - Only for light mode */}
      {!isDark && (
        <LinearGradient
          colors={['rgba(118, 192, 253, 0.48)', 'rgba(243, 243, 245, 1)']}
          start={{ x: 0.5, y: 0 }}
          end={{ x: 0.5, y: 1 }}
          style={styles.gradientBackground}
        />
      )}

      {/* Header */}
      <View style={styles.header}>
        <Text style={[styles.headerTitle, { color: COLORS.text }]}>
          {t('ai.title', 'AI Assistant')}
        </Text>
      </View>

      {/* Tab Navigation */}
      <View style={styles.tabContainer}>
        <TabButton
          label={t('ai.forecast', 'Forecast')}
          isActive={activeTab === 'forecast'}
          onPress={() => setActiveTab('forecast')}
          colors={COLORS}
        />
        <TabButton
          label={t('ai.maintenance', 'Maintenance')}
          isActive={activeTab === 'maintenance'}
          onPress={() => setActiveTab('maintenance')}
          colors={COLORS}
        />
      </View>

      {/* Tab Content */}
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {activeTab === 'forecast' ? (
          <View>
            {/* Hero Banner */}
            {showBanner && (
              <HeroBanner
                onClose={() => setShowBanner(false)}
              />
            )}

            {/* Feature Cards */}
            {forecastCards.map((card) => (
              <FeatureCard
                key={card.id}
                icon={card.icon}
                iconBgColor={card.iconBgColor}
                title={card.title}
                description={card.description}
                onPress={() => navigation.navigate(card.screen)}
                colors={COLORS}
                isDark={isDark}
              />
            ))}
          </View>
        ) : (
          <MaintenanceEmptyState colors={COLORS} />
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  gradientBackground: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: 300,
    zIndex: 0,
  },
  header: {
    paddingHorizontal: SPACING.lg,
    paddingTop: Platform.OS === 'ios' ? SPACING.xs : SPACING.md,
    paddingBottom: SPACING.xs,
  },
  headerTitle: {
    fontSize: TYPOGRAPHY.xxl,
    fontWeight: '700',
  },
  tabContainer: {
    flexDirection: 'row',
    paddingHorizontal: SPACING.lg,
    marginBottom: SPACING.md,
  },
  tabButton: {
    marginRight: SPACING.lg,
    paddingVertical: SPACING.xs,
  },
  tabText: {
    fontSize: TYPOGRAPHY.lg,
  },
  scrollContent: {
    paddingHorizontal: SPACING.lg,
    paddingBottom: SPACING.xxl * 2,
  },
  // Hero Banner
  heroBanner: {
    borderRadius: 16,
    height: 180,
    marginBottom: SPACING.md,
    overflow: 'hidden',
    position: 'relative',
  },
  heroImage: {
    width: '100%',
    height: '100%',
    position: 'absolute',
    top: 0,
    left: 0,
  },
  closeButton: {
    position: 'absolute',
    top: 12,
    right: 12,
    zIndex: 3,
    padding: 4,
    backgroundColor: 'rgba(255,255,255,0.8)',
    borderRadius: 12,
  },
  // Feature Cards
  featureCard: {
    borderRadius: 16,
    padding: SPACING.md,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: SPACING.sm,
  },
  featureCardContent: {
    flex: 1,
    paddingRight: SPACING.md,
  },
  featureCardTitle: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: '700',
    marginBottom: 4,
  },
  featureCardDescription: {
    fontSize: TYPOGRAPHY.xs,
  },
  featureCardVisual: {
    width: 64,
    height: 64,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
  },
  // Empty State
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: SPACING.xxl * 2,
  },
  emptyStateIcon: {
    width: 80,
    height: 80,
    borderRadius: 40,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: SPACING.lg,
  },
  emptyStateTitle: {
    fontSize: TYPOGRAPHY.lg,
    fontWeight: '600',
    marginBottom: SPACING.xs,
  },
  emptyStateDescription: {
    fontSize: TYPOGRAPHY.sm,
    textAlign: 'center',
  },
});
