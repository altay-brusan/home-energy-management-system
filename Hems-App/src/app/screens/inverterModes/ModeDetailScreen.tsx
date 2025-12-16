/**
 * ModeDetailScreen
 * Mod detay görüntüleme ve uygulama ekranı
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, CheckCircle, Settings, ChevronDown, ChevronUp } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useInverterModeStore } from '../../../store/inverterModeStore';
import { getModeById, getModeNumber } from '../../../data/presetModes';
import {
  SettingSection,
  SettingRow,
  TimelineBar,
} from '../../../components/inverterModes';
import {
  WORK_MODE_LABELS,
  ENERGY_PATTERN_LABELS,
  BATTERY_TYPE_LABELS,
} from '../../../api/types/inverterModes.types';

const ModeDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);

  const { activeModeId, selectMode, applyMode, customConfig } = useInverterModeStore();

  // State for showing/hiding settings
  const [showSettings, setShowSettings] = useState(false);

  const modeId = route.params?.modeId;
  const mode = getModeById(modeId);

  // Get translated mode name
  const getModeName = () => {
    if (!mode) return '';
    const modeKey = mode.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.name`, { defaultValue: mode.name });
  };

  const getModeDescription = () => {
    if (!mode) return '';
    const modeKey = mode.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.description`, { defaultValue: mode.description });
  };

  const getModeTargetUser = () => {
    if (!mode) return '';
    const modeKey = mode.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.targetUser`, { defaultValue: mode.targetUser });
  };

  const getModeRecommendedScenario = () => {
    if (!mode) return '';
    const modeKey = mode.id.replace('mode_', 'mode');
    return t(`inverterModes.modes.${modeKey}.recommendedScenario`, { defaultValue: mode.recommendedScenario });
  };

  if (!mode) {
    return (
      <View style={[styles.container, { backgroundColor: COLORS.dark, paddingTop: insets.top }]}>
        <Text style={{ color: COLORS.text }}>{t('inverterModes.modeNotFound')}</Text>
      </View>
    );
  }

  // Use custom config for custom mode, otherwise use preset config
  const config = mode.isCustom ? customConfig : mode.config;
  const isActive = activeModeId === modeId;

  const handleApplyMode = () => {
    const modeName = getModeName();
    Alert.alert(
      t('inverterModes.applyModeTitle'),
      t('inverterModes.applyModeConfirm', { mode: modeName }),
      [
        { text: t('inverterModes.cancel'), style: 'cancel' },
        {
          text: t('inverterModes.apply'),
          onPress: () => {
            selectMode(modeId);
            applyMode();
            Alert.alert(t('inverterModes.success'), t('inverterModes.modeAppliedSuccess', { mode: modeName }), [
              { text: t('inverterModes.ok'), onPress: () => navigation.goBack() },
            ]);
          },
        },
      ]
    );
  };

  const handleEditCustomMode = () => {
    navigation.navigate('CustomMode');
  };

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
      ]}
    >
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
        >
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]} numberOfLines={1}>
          {getModeName()}
        </Text>
        {mode.isCustom ? (
          <TouchableOpacity onPress={handleEditCustomMode} style={styles.editButton}>
            <Settings size={22} color={COLORS.accent} />
          </TouchableOpacity>
        ) : (
          <View style={{ width: 40 }} />
        )}
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 100 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Mode Badge and Status */}
        <View style={styles.badgeRow}>
          <View
            style={[
              styles.badge,
              {
                backgroundColor: mode.isCustom ? '#9C27B0' : COLORS.accent,
              },
            ]}
          >
            <Text style={styles.badgeText}>
              {mode.isCustom ? t('inverterModes.custom') : `${t('inverterModes.mode')} ${getModeNumber(mode.id)}`}
            </Text>
          </View>
          {isActive && (
            <View style={[styles.activeBadge, { backgroundColor: '#28A745' }]}>
              <CheckCircle size={14} color="#fff" />
              <Text style={styles.activeBadgeText}>{t('inverterModes.active')}</Text>
            </View>
          )}
        </View>

        {/* Description */}
        <Text style={[styles.description, { color: COLORS.text }]}>
          {getModeDescription()}
        </Text>

        {/* Info Cards */}
        <View style={styles.infoCardsRow}>
          <View
            style={[
              styles.infoCard,
              { backgroundColor: isDark ? COLORS.surface : COLORS.white },
            ]}
          >
            <Text style={[styles.infoCardLabel, { color: COLORS.textSoft }]}>
              {t('inverterModes.targetUser')}
            </Text>
            <Text style={[styles.infoCardValue, { color: COLORS.text }]}>
              {getModeTargetUser()}
            </Text>
          </View>
          <View
            style={[
              styles.infoCard,
              { backgroundColor: isDark ? COLORS.surface : COLORS.white },
            ]}
          >
            <Text style={[styles.infoCardLabel, { color: COLORS.textSoft }]}>
              {t('inverterModes.recommendedScenario')}
            </Text>
            <Text style={[styles.infoCardValue, { color: COLORS.text }]}>
              {getModeRecommendedScenario()}
            </Text>
          </View>
        </View>

        {/* Show/Hide Settings Button */}
        <TouchableOpacity
          onPress={() => setShowSettings(!showSettings)}
          style={[
            styles.showSettingsButton,
            { backgroundColor: isDark ? COLORS.surface : COLORS.white },
          ]}
        >
          <Text style={[styles.showSettingsButtonText, { color: COLORS.accent }]}>
            {showSettings ? t('inverterModes.hideSettings') : t('inverterModes.showSettings')}
          </Text>
          {showSettings ? (
            <ChevronUp size={20} color={COLORS.accent} />
          ) : (
            <ChevronDown size={20} color={COLORS.accent} />
          )}
        </TouchableOpacity>

        {/* Settings Sections - Only show when expanded */}
        {showSettings && (
        <>
        {/* Settings Sections */}
        <Text style={[styles.sectionTitle, { color: COLORS.text, marginTop: 16 }]}>
          {t('inverterModes.settingDetails')}
        </Text>

        {/* Work Mode Settings */}
        <SettingSection title={t('inverterModes.settings.workMode')} icon="cog" defaultExpanded>
          <SettingRow
            label="Work Mode"
            value={WORK_MODE_LABELS[config.workMode.workMode]}
          />
          <SettingRow
            label="Solar Sell"
            value=""
            status={config.workMode.solarSell}
          />
          <SettingRow
            label="Energy Pattern"
            value={ENERGY_PATTERN_LABELS[config.workMode.energyPattern]}
          />
          <SettingRow
            label="Max Sell Power"
            value={config.workMode.maxSellPower}
            unit="W"
          />
          <SettingRow
            label="Zero Export Power"
            value={config.workMode.zeroExportPower}
            unit="W"
          />
          <SettingRow
            label="Grid Peak Shaving"
            value=""
            status={config.workMode.gridPeakShaving}
          />
          {config.workMode.gridPeakShaving === 'on' && (
            <SettingRow
              label="Peak Shaving Power"
              value={config.workMode.gridPeakShavingPower}
              unit="W"
            />
          )}
          <SettingRow
            label="Time of Use"
            value=""
            status={config.workMode.timeOfUse}
          />
        </SettingSection>

        {/* Battery Settings */}
        <SettingSection title={t('inverterModes.settings.battery')} icon="battery-full">
          <SettingRow
            label={t('inverterModes.labels.batteryType')}
            value={BATTERY_TYPE_LABELS[config.battery.batteryType]}
          />
          <SettingRow
            label={t('inverterModes.labels.batteryCapacity')}
            value={config.battery.batteryCapacity}
            unit="Ah"
          />
          <SettingRow
            label={t('inverterModes.labels.maxChargeCurrent')}
            value={config.battery.maxACharge}
            unit="A"
          />
          <SettingRow
            label={t('inverterModes.labels.maxDischargeCurrent')}
            value={config.battery.maxADischarge}
            unit="A"
          />
          <SettingRow
            label="Shutdown SOC"
            value={config.battery.shutdownSOC}
            unit="%"
            highlight
          />
          <SettingRow
            label="Low Batt SOC"
            value={config.battery.lowBattSOC}
            unit="%"
            highlight
          />
          <SettingRow
            label="Restart SOC"
            value={config.battery.restartSOC}
            unit="%"
            highlight
          />
        </SettingSection>

        {/* Grid Settings */}
        <SettingSection title={t('inverterModes.settings.grid')} icon="plug">
          <SettingRow
            label="Grid Charge"
            value=""
            status={config.grid.gridCharge}
          />
          {config.grid.gridCharge === 'on' && (
            <>
              <SettingRow
                label="Grid Charge SOC"
                value={config.grid.gridChargeSOC}
                unit="%"
              />
              <SettingRow
                label="Grid Charge Current"
                value={config.grid.gridChargeCurrent}
                unit="A"
              />
            </>
          )}
          <SettingRow
            label="Grid Signal"
            value=""
            status={config.grid.gridSignal}
          />
        </SettingSection>

        {/* Generator Settings */}
        <SettingSection title={t('inverterModes.settings.generator')} icon="bolt">
          <SettingRow
            label="Gen Charge"
            value=""
            status={config.generator.genCharge}
          />
          {config.generator.genCharge === 'on' && (
            <>
              <SettingRow
                label="Gen Start SOC"
                value={config.generator.genStartSOC}
                unit="%"
              />
              <SettingRow
                label="Gen Charge Current"
                value={config.generator.genChargeCurrent}
                unit="A"
              />
              <SettingRow
                label={t('inverterModes.labels.genMaxRunTime')}
                value={config.generator.genMaxRunTime}
                unit={t('inverterModes.hours')}
              />
              <SettingRow
                label={t('inverterModes.labels.genDownTime')}
                value={config.generator.genDownTime}
                unit={t('inverterModes.hours')}
              />
              <SettingRow
                label="Rated Power"
                value={config.generator.genRatedPower}
                unit="W"
              />
            </>
          )}
          <SettingRow
            label="Gen Signal"
            value=""
            status={config.generator.genSignal}
          />
          <SettingRow
            label="Gen Peak Shaving"
            value=""
            status={config.generator.genPeakShaving}
          />
        </SettingSection>

        {/* Advanced Settings */}
        <SettingSection title={t('inverterModes.settings.advanced')} icon="sliders">
          <SettingRow
            label="Signal Island Mode"
            value=""
            status={config.advanced.signalIslandMode}
          />
          <SettingRow
            label="BMS Error Stop"
            value=""
            status={config.advanced.bmsErrStop}
          />
          <SettingRow
            label="Backup Delay"
            value={config.advanced.backupDelay}
            unit="ms"
          />
          <SettingRow label="CT Ratio" value={config.advanced.ctRatio} />
        </SettingSection>

        {/* Time of Use */}
        {config.timeOfUse.enabled === 'on' && config.timeOfUse.periods.length > 0 && (
          <SettingSection title={t('inverterModes.settings.timeOfUse')} icon="clock-o" defaultExpanded>
            <TimelineBar periods={config.timeOfUse.periods} />
            {config.timeOfUse.periods.map((period) => (
              <View
                key={period.id}
                style={[
                  styles.periodItem,
                  { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
                ]}
              >
                <Text style={[styles.periodTime, { color: COLORS.accent }]}>
                  {period.startTime} - {period.endTime}
                </Text>
                <Text style={[styles.periodDesc, { color: COLORS.textSoft }]}>
                  {`SOC: ${period.targetSOC}%, Max: ${period.maxDischargePower}W`}
                </Text>
              </View>
            ))}
          </SettingSection>
        )}
        </>
        )}

        {/* Edit Button for Custom Mode */}
        {mode.isCustom && (
          <TouchableOpacity
            onPress={handleEditCustomMode}
            style={[styles.editModeButton, { backgroundColor: '#9C27B0' }]}
          >
            <Settings size={20} color="#fff" />
            <Text style={styles.editModeButtonText}>{t('inverterModes.editSettingsButton')}</Text>
          </TouchableOpacity>
        )}
      </ScrollView>

      {/* Apply Button */}
      <View
        style={[
          styles.footer,
          {
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            paddingBottom: insets.bottom + 16,
          },
        ]}
      >
        <TouchableOpacity
          onPress={handleApplyMode}
          disabled={isActive}
          style={[
            styles.applyButton,
            {
              backgroundColor: isActive ? COLORS.border : COLORS.accent,
            },
          ]}
        >
          {isActive ? (
            <>
              <CheckCircle size={20} color={COLORS.textSoft} />
              <Text style={[styles.applyButtonText, { color: COLORS.textSoft }]}>
                {t('inverterModes.thisModeActive')}
              </Text>
            </>
          ) : (
            <Text style={styles.applyButtonText}>{t('inverterModes.applyThisMode')}</Text>
          )}
        </TouchableOpacity>
      </View>
    </View>
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
  editButton: {
    padding: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    textAlign: 'center',
    marginHorizontal: 8,
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  badgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 12,
  },
  badge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  badgeText: {
    color: '#fff',
    fontSize: 13,
    fontWeight: '700',
  },
  activeBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 16,
    gap: 4,
  },
  activeBadgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  description: {
    fontSize: 15,
    lineHeight: 22,
    marginBottom: 16,
  },
  infoCardsRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 24,
  },
  infoCard: {
    flex: 1,
    borderRadius: 12,
    padding: 14,
  },
  infoCardLabel: {
    fontSize: 11,
    marginBottom: 4,
  },
  infoCardValue: {
    fontSize: 13,
    fontWeight: '600',
  },
  showSettingsButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 14,
    borderRadius: 12,
    marginBottom: 8,
    gap: 8,
  },
  showSettingsButtonText: {
    fontSize: 15,
    fontWeight: '600',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 12,
  },
  periodItem: {
    paddingVertical: 10,
    borderBottomWidth: 1,
  },
  periodTime: {
    fontSize: 14,
    fontWeight: '700',
  },
  periodDesc: {
    fontSize: 12,
    marginTop: 2,
  },
  editModeButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 14,
    borderRadius: 12,
    marginTop: 16,
    gap: 8,
  },
  editModeButtonText: {
    color: '#fff',
    fontSize: 15,
    fontWeight: '600',
  },
  footer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    paddingHorizontal: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.1)',
  },
  applyButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    gap: 8,
  },
  applyButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default ModeDetailScreen;
