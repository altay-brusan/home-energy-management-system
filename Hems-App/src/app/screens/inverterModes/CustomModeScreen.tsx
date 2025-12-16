/**
 * CustomModeScreen
 * Özel mod ayarlarını düzenleme ekranı
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  SafeAreaView,
  Alert,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Save, RotateCcw, Clock } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useInverterModeStore } from '../../../store/inverterModeStore';
import {
  SettingSection,
  SettingToggle,
  SettingSlider,
  SettingPicker,
} from '../../../components/inverterModes';
import {
  WorkModeType,
  EnergyPatternType,
  BatteryType,
  VALIDATION_RULES,
} from '../../../api/types/inverterModes.types';

const WORK_MODE_OPTIONS = [
  { value: 'selling_first', label: 'Selling First' },
  { value: 'zero_export_to_load', label: 'Zero Export to Load' },
  { value: 'zero_export_to_ct', label: 'Zero Export to CT' },
];

const ENERGY_PATTERN_OPTIONS = [
  { value: 'batt_first', label: 'Battery First' },
  { value: 'load_first', label: 'Load First' },
];

const BATTERY_TYPE_OPTIONS = [
  { value: 'lithium', label: 'Lithium' },
  { value: 'agm', label: 'AGM' },
  { value: 'gel', label: 'Gel' },
  { value: 'flooded', label: 'Flooded' },
];

const CustomModeScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    customConfig,
    updateWorkModeSettings,
    updateBatterySettings,
    updateGridSettings,
    updateGeneratorSettings,
    updateAdvancedSettings,
    updateTimeOfUseSettings,
    selectMode,
    applyMode,
  } = useInverterModeStore();

  const [hasChanges, setHasChanges] = useState(false);

  const markChanged = () => setHasChanges(true);

  const handleSave = () => {
    // Validate SOC values
    const { shutdownSOC, lowBattSOC, restartSOC } = customConfig.battery;
    if (shutdownSOC >= lowBattSOC || lowBattSOC >= restartSOC) {
      Alert.alert(
        t('inverterModes.validationError'),
        t('inverterModes.socValidation')
      );
      return;
    }

    selectMode('custom');
    applyMode();
    setHasChanges(false);
    Alert.alert(t('inverterModes.success'), t('inverterModes.customModeApplied'), [
      { text: t('inverterModes.ok'), onPress: () => navigation.goBack() },
    ]);
  };

  const handleReset = () => {
    Alert.alert(
      t('inverterModes.resetSettings'),
      t('inverterModes.resetConfirm'),
      [
        { text: t('inverterModes.cancel'), style: 'cancel' },
        {
          text: t('inverterModes.reset'),
          style: 'destructive',
          onPress: () => {
            // Reset to default config - implement in store if needed
            setHasChanges(false);
          },
        },
      ]
    );
  };

  const navigateToTimeOfUse = () => {
    navigation.navigate('TimeOfUse');
  };

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
          {t('inverterModes.customModeSettings')}
        </Text>
        <TouchableOpacity onPress={handleReset} style={styles.resetButton}>
          <RotateCcw size={20} color={COLORS.textSoft} />
        </TouchableOpacity>
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 100 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Work Mode Settings */}
        <SettingSection title={t('inverterModes.settings.workMode')} icon="cog" defaultExpanded>
          <SettingPicker
            label="Work Mode"
            value={customConfig.workMode.workMode}
            options={WORK_MODE_OPTIONS}
            onChange={(value) => {
              updateWorkModeSettings({ workMode: value as WorkModeType });
              markChanged();
            }}
          />
          <SettingToggle
            label="Solar Sell"
            value={customConfig.workMode.solarSell}
            onChange={(value) => {
              updateWorkModeSettings({ solarSell: value });
              markChanged();
            }}
            description={t('inverterModes.descriptions.solarSell')}
          />
          <SettingPicker
            label="Energy Pattern"
            value={customConfig.workMode.energyPattern}
            options={ENERGY_PATTERN_OPTIONS}
            onChange={(value) => {
              updateWorkModeSettings({ energyPattern: value as EnergyPatternType });
              markChanged();
            }}
          />
          <SettingSlider
            label="Max Sell Power"
            value={customConfig.workMode.maxSellPower}
            onChange={(value) => {
              updateWorkModeSettings({ maxSellPower: value });
              markChanged();
            }}
            min={VALIDATION_RULES.power.maxSellPower.min}
            max={VALIDATION_RULES.power.maxSellPower.max}
            step={VALIDATION_RULES.power.maxSellPower.step}
            unit="W"
          />
          <SettingSlider
            label="Zero Export Power"
            value={customConfig.workMode.zeroExportPower}
            onChange={(value) => {
              updateWorkModeSettings({ zeroExportPower: value });
              markChanged();
            }}
            min={VALIDATION_RULES.power.zeroExportPower.min}
            max={VALIDATION_RULES.power.zeroExportPower.max}
            step={VALIDATION_RULES.power.zeroExportPower.step}
            unit="W"
          />
          <SettingToggle
            label="Grid Peak Shaving"
            value={customConfig.workMode.gridPeakShaving}
            onChange={(value) => {
              updateWorkModeSettings({ gridPeakShaving: value });
              markChanged();
            }}
          />
          {customConfig.workMode.gridPeakShaving === 'on' && (
            <SettingSlider
              label="Peak Shaving Power"
              value={customConfig.workMode.gridPeakShavingPower}
              onChange={(value) => {
                updateWorkModeSettings({ gridPeakShavingPower: value });
                markChanged();
              }}
              min={VALIDATION_RULES.power.gridPeakShaving.min}
              max={VALIDATION_RULES.power.gridPeakShaving.max}
              step={VALIDATION_RULES.power.gridPeakShaving.step}
              unit="W"
            />
          )}
          <SettingToggle
            label="Time of Use"
            value={customConfig.workMode.timeOfUse}
            onChange={(value) => {
              updateWorkModeSettings({ timeOfUse: value });
              markChanged();
            }}
          />
        </SettingSection>

        {/* Time of Use Button */}
        {customConfig.workMode.timeOfUse === 'on' && (
          <TouchableOpacity
            onPress={navigateToTimeOfUse}
            style={[
              styles.timeOfUseButton,
              { backgroundColor: isDark ? COLORS.surface : COLORS.white },
            ]}
          >
            <Clock size={20} color={COLORS.accent} />
            <Text style={[styles.timeOfUseButtonText, { color: COLORS.text }]}>
              {t('inverterModes.editTimeOfUse')}
            </Text>
            <Text style={[styles.periodCount, { color: COLORS.textSoft }]}>
              {t('inverterModes.periodCount', { count: customConfig.timeOfUse.periods.length })}
            </Text>
          </TouchableOpacity>
        )}

        {/* Battery Settings */}
        <SettingSection title={t('inverterModes.settings.battery')} icon="battery-full">
          <SettingPicker
            label={t('inverterModes.labels.batteryType')}
            value={customConfig.battery.batteryType}
            options={BATTERY_TYPE_OPTIONS}
            onChange={(value) => {
              updateBatterySettings({ batteryType: value as BatteryType });
              markChanged();
            }}
          />
          <SettingSlider
            label={t('inverterModes.labels.batteryCapacity')}
            value={customConfig.battery.batteryCapacity}
            onChange={(value) => {
              updateBatterySettings({ batteryCapacity: value });
              markChanged();
            }}
            min={VALIDATION_RULES.capacity.battery.min}
            max={VALIDATION_RULES.capacity.battery.max}
            step={VALIDATION_RULES.capacity.battery.step}
            unit="Ah"
          />
          <SettingSlider
            label={t('inverterModes.labels.maxChargeCurrent')}
            value={customConfig.battery.maxACharge}
            onChange={(value) => {
              updateBatterySettings({ maxACharge: value });
              markChanged();
            }}
            min={VALIDATION_RULES.current.maxCharge.min}
            max={VALIDATION_RULES.current.maxCharge.max}
            step={VALIDATION_RULES.current.maxCharge.step}
            unit="A"
          />
          <SettingSlider
            label={t('inverterModes.labels.maxDischargeCurrent')}
            value={customConfig.battery.maxADischarge}
            onChange={(value) => {
              updateBatterySettings({ maxADischarge: value });
              markChanged();
            }}
            min={VALIDATION_RULES.current.maxDischarge.min}
            max={VALIDATION_RULES.current.maxDischarge.max}
            step={VALIDATION_RULES.current.maxDischarge.step}
            unit="A"
          />
          <SettingSlider
            label={t('inverterModes.labels.shutdownSOC')}
            value={customConfig.battery.shutdownSOC}
            onChange={(value) => {
              updateBatterySettings({ shutdownSOC: value });
              markChanged();
            }}
            min={0}
            max={30}
            step={1}
            unit="%"
            description={t('inverterModes.descriptions.shutdownSOC')}
          />
          <SettingSlider
            label={t('inverterModes.labels.lowBattSOC')}
            value={customConfig.battery.lowBattSOC}
            onChange={(value) => {
              updateBatterySettings({ lowBattSOC: value });
              markChanged();
            }}
            min={10}
            max={50}
            step={1}
            unit="%"
            description={t('inverterModes.descriptions.lowBattSOC')}
          />
          <SettingSlider
            label={t('inverterModes.labels.restartSOC')}
            value={customConfig.battery.restartSOC}
            onChange={(value) => {
              updateBatterySettings({ restartSOC: value });
              markChanged();
            }}
            min={20}
            max={100}
            step={1}
            unit="%"
            description={t('inverterModes.descriptions.restartSOC')}
          />
        </SettingSection>

        {/* Grid Settings */}
        <SettingSection title={t('inverterModes.settings.grid')} icon="plug">
          <SettingToggle
            label={t('inverterModes.labels.gridCharge')}
            value={customConfig.grid.gridCharge}
            onChange={(value) => {
              updateGridSettings({ gridCharge: value });
              markChanged();
            }}
            description={t('inverterModes.descriptions.gridCharge')}
          />
          {customConfig.grid.gridCharge === 'on' && (
            <>
              <SettingSlider
                label="Grid Charge SOC"
                value={customConfig.grid.gridChargeSOC}
                onChange={(value) => {
                  updateGridSettings({ gridChargeSOC: value });
                  markChanged();
                }}
                min={0}
                max={100}
                step={5}
                unit="%"
              />
              <SettingSlider
                label="Grid Charge Current"
                value={customConfig.grid.gridChargeCurrent}
                onChange={(value) => {
                  updateGridSettings({ gridChargeCurrent: value });
                  markChanged();
                }}
                min={VALIDATION_RULES.current.gridCharge.min}
                max={VALIDATION_RULES.current.gridCharge.max}
                step={VALIDATION_RULES.current.gridCharge.step}
                unit="A"
              />
            </>
          )}
          <SettingToggle
            label="Grid Signal"
            value={customConfig.grid.gridSignal}
            onChange={(value) => {
              updateGridSettings({ gridSignal: value });
              markChanged();
            }}
          />
        </SettingSection>

        {/* Generator Settings */}
        <SettingSection title={t('inverterModes.settings.generator')} icon="bolt">
          <SettingToggle
            label={t('inverterModes.labels.genCharge')}
            value={customConfig.generator.genCharge}
            onChange={(value) => {
              updateGeneratorSettings({ genCharge: value });
              markChanged();
            }}
            description={t('inverterModes.descriptions.genCharge')}
          />
          {customConfig.generator.genCharge === 'on' && (
            <>
              <SettingSlider
                label="Gen Start SOC"
                value={customConfig.generator.genStartSOC}
                onChange={(value) => {
                  updateGeneratorSettings({ genStartSOC: value });
                  markChanged();
                }}
                min={0}
                max={100}
                step={5}
                unit="%"
              />
              <SettingSlider
                label="Gen Charge Current"
                value={customConfig.generator.genChargeCurrent}
                onChange={(value) => {
                  updateGeneratorSettings({ genChargeCurrent: value });
                  markChanged();
                }}
                min={VALIDATION_RULES.current.genCharge.min}
                max={VALIDATION_RULES.current.genCharge.max}
                step={VALIDATION_RULES.current.genCharge.step}
                unit="A"
              />
              <SettingSlider
                label={t('inverterModes.labels.genMaxRunTime')}
                value={customConfig.generator.genMaxRunTime}
                onChange={(value) => {
                  updateGeneratorSettings({ genMaxRunTime: value });
                  markChanged();
                }}
                min={VALIDATION_RULES.time.genMaxRunTime.min}
                max={VALIDATION_RULES.time.genMaxRunTime.max}
                step={VALIDATION_RULES.time.genMaxRunTime.step}
                unit={t('inverterModes.hours')}
              />
              <SettingSlider
                label={t('inverterModes.labels.genDownTime')}
                value={customConfig.generator.genDownTime}
                onChange={(value) => {
                  updateGeneratorSettings({ genDownTime: value });
                  markChanged();
                }}
                min={VALIDATION_RULES.time.genDownTime.min}
                max={VALIDATION_RULES.time.genDownTime.max}
                step={VALIDATION_RULES.time.genDownTime.step}
                unit={t('inverterModes.hours')}
              />
              <SettingSlider
                label="Rated Power"
                value={customConfig.generator.genRatedPower}
                onChange={(value) => {
                  updateGeneratorSettings({ genRatedPower: value });
                  markChanged();
                }}
                min={VALIDATION_RULES.capacity.genRated.min}
                max={VALIDATION_RULES.capacity.genRated.max}
                step={VALIDATION_RULES.capacity.genRated.step}
                unit="W"
              />
            </>
          )}
          <SettingToggle
            label="Gen Signal"
            value={customConfig.generator.genSignal}
            onChange={(value) => {
              updateGeneratorSettings({ genSignal: value });
              markChanged();
            }}
          />
          <SettingToggle
            label="Gen Peak Shaving"
            value={customConfig.generator.genPeakShaving}
            onChange={(value) => {
              updateGeneratorSettings({ genPeakShaving: value });
              markChanged();
            }}
          />
        </SettingSection>

        {/* Advanced Settings */}
        <SettingSection title={t('inverterModes.settings.advanced')} icon="sliders">
          <SettingToggle
            label={t('inverterModes.labels.signalIslandMode')}
            value={customConfig.advanced.signalIslandMode}
            onChange={(value) => {
              updateAdvancedSettings({ signalIslandMode: value });
              markChanged();
            }}
            description={t('inverterModes.descriptions.signalIslandMode')}
          />
          <SettingToggle
            label={t('inverterModes.labels.bmsErrStop')}
            value={customConfig.advanced.bmsErrStop}
            onChange={(value) => {
              updateAdvancedSettings({ bmsErrStop: value });
              markChanged();
            }}
            description={t('inverterModes.descriptions.bmsErrStop')}
          />
          <SettingSlider
            label="Backup Delay"
            value={customConfig.advanced.backupDelay}
            onChange={(value) => {
              updateAdvancedSettings({ backupDelay: value });
              markChanged();
            }}
            min={VALIDATION_RULES.time.backupDelay.min}
            max={VALIDATION_RULES.time.backupDelay.max}
            step={VALIDATION_RULES.time.backupDelay.step}
            unit="ms"
          />
        </SettingSection>
      </ScrollView>

      {/* Save Button */}
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
          onPress={handleSave}
          style={[styles.saveButton, { backgroundColor: COLORS.accent }]}
        >
          <Save size={20} color="#fff" />
          <Text style={styles.saveButtonText}>{t('inverterModes.saveAndApply')}</Text>
        </TouchableOpacity>
      </View>
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
  resetButton: {
    padding: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    textAlign: 'center',
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  timeOfUseButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    gap: 12,
  },
  timeOfUseButtonText: {
    fontSize: 14,
    fontWeight: '600',
    flex: 1,
  },
  periodCount: {
    fontSize: 12,
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
  saveButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    gap: 8,
  },
  saveButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default CustomModeScreen;
