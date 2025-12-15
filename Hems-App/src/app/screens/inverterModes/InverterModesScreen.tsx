/**
 * InverterModesScreen
 * 9 mod kartlı grid görünüm (3 sütun)
 */

import React from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  SafeAreaView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react-native';
import Reanimated, { FadeInDown } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useInverterModeStore } from '../../../store/inverterModeStore';
import { PRESET_MODES } from '../../../data/presetModes';
import { ModeCard } from '../../../components/inverterModes';

const InverterModesScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const { activeModeId } = useInverterModeStore();

  const handleModePress = (modeId: string) => {
    // Only navigate to detail screen, don't select mode here
    // Mode will be activated from detail screen with confirmation
    navigation.navigate('ModeDetail', { modeId });
  };

  // Get translated mode name
  const getModeName = (modeItem: typeof PRESET_MODES[0]) => {
    const modeKey = modeItem.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.name`, { defaultValue: modeItem.name });
  };

  const getModeDescription = (modeItem: typeof PRESET_MODES[0]) => {
    const modeKey = modeItem.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.description`, { defaultValue: modeItem.description });
  };

  // Separate preset modes and custom mode
  const presetModes = PRESET_MODES.filter((m) => !m.isCustom);
  const customMode = PRESET_MODES.find((m) => m.isCustom);

  return (
    <SafeAreaView
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
      ]}
    >
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
        >
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>
          {t('inverterModes.title')}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 20 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Active Mode Info */}
        {activeModeId && (
          <Reanimated.View
            entering={FadeInDown.duration(400)}
            style={[
              styles.activeCard,
              { backgroundColor: isDark ? COLORS.surface : COLORS.white },
            ]}
          >
            <Text style={[styles.activeLabel, { color: COLORS.textSoft }]}>
              {t('inverterModes.activeMode')}
            </Text>
            <Text style={[styles.activeName, { color: COLORS.accent }]}>
              {PRESET_MODES.find((m) => m.id === activeModeId)
                ? getModeName(PRESET_MODES.find((m) => m.id === activeModeId)!)
                : t('inverterModes.noModeSelected')}
            </Text>
          </Reanimated.View>
        )}

        {/* Description */}
        <Text style={[styles.description, { color: COLORS.textSoft }]}>
          {t('inverterModes.subtitle')}
        </Text>

        {/* Section Title - Preset Modes */}
        <Text style={[styles.sectionTitle, { color: COLORS.text }]}>
          {t('inverterModes.presetModes', { defaultValue: 'Preset Modes' })}
        </Text>

        {/* Preset Modes Grid */}
        <View style={styles.grid}>
          {presetModes.map((item, index) => (
            <Reanimated.View
              key={item.id}
              entering={FadeInDown.delay(index * 50).duration(400)}
            >
              <ModeCard
                id={item.id}
                name={getModeName(item)}
                description={getModeDescription(item)}
                isActive={activeModeId === item.id}
                isCustom={item.isCustom}
                onPress={() => handleModePress(item.id)}
              />
            </Reanimated.View>
          ))}
        </View>

        {/* Section Title - Custom Mode */}
        <Text style={[styles.sectionTitle, { color: COLORS.text, marginTop: 24 }]}>
          {t('inverterModes.custom')}
        </Text>

        {/* Custom Mode Card - Full Width */}
        {customMode && (
          <Reanimated.View entering={FadeInDown.delay(400).duration(400)}>
            <TouchableOpacity
              onPress={() => handleModePress(customMode.id)}
              activeOpacity={0.7}
              style={[
                styles.customCard,
                {
                  backgroundColor: isDark ? COLORS.surface : COLORS.white,
                  borderColor:
                    activeModeId === customMode.id ? '#9C27B0' : COLORS.border,
                  borderWidth: activeModeId === customMode.id ? 2 : 1,
                  borderStyle: 'dashed',
                },
              ]}
            >
              <View style={styles.customBadge}>
                <Text style={styles.customBadgeText}>{t('inverterModes.custom')}</Text>
              </View>
              <Text style={[styles.customTitle, { color: COLORS.text }]}>
                {t('inverterModes.modes.custom.name')}
              </Text>
              <Text style={[styles.customDesc, { color: COLORS.textSoft }]}>
                {t('inverterModes.modes.custom.description')}
              </Text>
              {activeModeId === customMode.id && (
                <View style={styles.activeIndicator}>
                  <Text style={styles.activeIndicatorText}>
                    {t('inverterModes.activeMode', { defaultValue: 'Active' })}
                  </Text>
                </View>
              )}
            </TouchableOpacity>
          </Reanimated.View>
        )}

        {/* Info Box */}
        <View
          style={[
            styles.infoBox,
            { backgroundColor: isDark ? `${COLORS.accent}15` : '#E3F2FD' },
          ]}
        >
          <Text style={[styles.infoText, { color: COLORS.textSoft }]}>
            {t('inverterModes.infoText', {
              defaultValue: 'Tap on a mode to view details and apply it. Use the custom mode option to manually configure all settings.'
            })}
          </Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  backButton: {
    padding: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  activeCard: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    alignItems: 'center',
  },
  activeLabel: {
    fontSize: 12,
    marginBottom: 4,
  },
  activeName: {
    fontSize: 18,
    fontWeight: '700',
  },
  description: {
    fontSize: 14,
    lineHeight: 20,
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 12,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginHorizontal: -8,
  },
  customCard: {
    borderRadius: 12,
    padding: 20,
    marginBottom: 16,
    position: 'relative',
  },
  customBadge: {
    backgroundColor: '#9C27B0',
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
    alignSelf: 'flex-start',
    marginBottom: 12,
  },
  customBadgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '700',
  },
  customTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 4,
  },
  customDesc: {
    fontSize: 14,
    lineHeight: 20,
  },
  activeIndicator: {
    position: 'absolute',
    top: 16,
    right: 16,
    backgroundColor: '#9C27B0',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 10,
  },
  activeIndicatorText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '600',
  },
  infoBox: {
    borderRadius: 12,
    padding: 16,
    marginTop: 8,
  },
  infoText: {
    fontSize: 13,
    lineHeight: 18,
  },
});

export default InverterModesScreen;
