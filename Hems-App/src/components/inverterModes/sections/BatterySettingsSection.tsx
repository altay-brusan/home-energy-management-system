/**
 * BatterySettingsSection Component
 * API sortOrder: 1 - BatterySettings kategorisi
 * Tek bir section içinde tüm battery ayarları
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useBatterySettingsSection } from '../../../hooks/useDeviceSettings';
import { DEVICE_SETTINGS_VALIDATION } from '../../../api/types/inverterModes.types';
import SettingSection from '../SettingSection';
import SettingToggle from '../SettingToggle';
import SettingSlider from '../SettingSlider';

interface BatterySettingsSectionProps {
  defaultExpanded?: boolean;
}

const BatterySettingsSection: React.FC<BatterySettingsSectionProps> = ({
  defaultExpanded = false,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    batteryConfiguration,
    gridCharging,
    generatorCharging,
    protectionThresholds,
    updateBatteryConfiguration,
    updateGridCharging,
    updateGeneratorCharging,
    updateProtectionThresholds,
    isLoading,
  } = useBatterySettingsSection();

  const validation = DEVICE_SETTINGS_VALIDATION;

  if (isLoading) {
    return null;
  }

  return (
    <SettingSection
      title={t('inverterModes.settings.batterySettings', 'Battery Settings')}
      icon="battery-full"
      defaultExpanded={defaultExpanded}
    >
      {/* Battery Configuration */}
      <Text style={[styles.subHeader, { color: COLORS.text }]}>
        {t('inverterModes.settings.batteryConfiguration', 'Battery Configuration')}
      </Text>
      <SettingSlider
        label={t('inverterModes.labels.batteryCapacity', 'Battery Capacity')}
        value={batteryConfiguration.capacity}
        onChange={(value) => updateBatteryConfiguration({ capacity: value })}
        min={validation.batteryConfig.capacity.min}
        max={validation.batteryConfig.capacity.max}
        step={validation.batteryConfig.capacity.step}
        unit="Ah"
      />
      <SettingSlider
        label={t('inverterModes.labels.maxBatteryChargeCurrent', 'Max Charge Current')}
        value={batteryConfiguration.maxChargeCurrent}
        onChange={(value) => updateBatteryConfiguration({ maxChargeCurrent: value })}
        min={validation.batteryConfig.maxChargeCurrent.min}
        max={validation.batteryConfig.maxChargeCurrent.max}
        step={validation.batteryConfig.maxChargeCurrent.step}
        unit="A"
      />
      <SettingSlider
        label={t('inverterModes.labels.maxBatteryDischargeCurrent', 'Max Discharge Current')}
        value={batteryConfiguration.maxDischargeCurrent}
        onChange={(value) => updateBatteryConfiguration({ maxDischargeCurrent: value })}
        min={validation.batteryConfig.maxDischargeCurrent.min}
        max={validation.batteryConfig.maxDischargeCurrent.max}
        step={validation.batteryConfig.maxDischargeCurrent.step}
        unit="A"
      />

      {/* Grid Charging */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.gridCharging', 'Grid Charging')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.gridChargingEnabled', 'Enable Grid Charging')}
        value={gridCharging.enabled}
        onChange={(value) => updateGridCharging({ enabled: value })}
      />
      {gridCharging.enabled === 'on' && (
        <>
          <SettingSlider
            label={t('inverterModes.labels.gridChargingStartSOC', 'Start Charging SOC')}
            value={gridCharging.startSOC}
            onChange={(value) => updateGridCharging({ startSOC: value })}
            min={validation.batterySOC.gridChargingStart.min}
            max={validation.batterySOC.gridChargingStart.max}
            step={validation.batterySOC.gridChargingStart.step}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.gridChargingCurrent', 'Charging Current')}
            value={gridCharging.current}
            onChange={(value) => updateGridCharging({ current: value })}
            min={validation.current.gridCharge.min}
            max={validation.current.gridCharge.max}
            step={validation.current.gridCharge.step}
            unit="A"
          />
        </>
      )}

      {/* Generator Charging */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.generatorCharging', 'Generator Charging')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.generatorChargingEnabled', 'Enable Generator Charging')}
        value={generatorCharging.enabled}
        onChange={(value) => updateGeneratorCharging({ enabled: value })}
      />
      {generatorCharging.enabled === 'on' && (
        <>
          <SettingSlider
            label={t('inverterModes.labels.generatorStartSOC', 'Start Charging SOC')}
            value={generatorCharging.startSOC}
            onChange={(value) => updateGeneratorCharging({ startSOC: value })}
            min={validation.batterySOC.generatorStart.min}
            max={validation.batterySOC.generatorStart.max}
            step={validation.batterySOC.generatorStart.step}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.generatorChargingCurrent', 'Charging Current')}
            value={generatorCharging.current}
            onChange={(value) => updateGeneratorCharging({ current: value })}
            min={validation.current.genCharge.min}
            max={validation.current.genCharge.max}
            step={validation.current.genCharge.step}
            unit="A"
          />
        </>
      )}

      {/* Protection Thresholds */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.protectionThresholds', 'Protection Thresholds')}
      </Text>
      <SettingSlider
        label={t('inverterModes.labels.shutdownSOC', 'Shutdown SOC')}
        value={protectionThresholds.shutdownSOC}
        onChange={(value) => updateProtectionThresholds({ shutdownSOC: value })}
        min={validation.batterySOC.shutdown.min}
        max={validation.batterySOC.shutdown.max}
        step={validation.batterySOC.shutdown.step}
        unit="%"
      />
      <SettingSlider
        label={t('inverterModes.labels.lowBatteryWarning', 'Low Battery Warning')}
        value={protectionThresholds.lowBatteryWarning}
        onChange={(value) => updateProtectionThresholds({ lowBatteryWarning: value })}
        min={validation.batterySOC.lowBattery.min}
        max={validation.batterySOC.lowBattery.max}
        step={validation.batterySOC.lowBattery.step}
        unit="%"
      />
      <SettingSlider
        label={t('inverterModes.labels.restartSOC', 'Restart SOC')}
        value={protectionThresholds.restartSOC}
        onChange={(value) => updateProtectionThresholds({ restartSOC: value })}
        min={validation.batterySOC.restart.min}
        max={validation.batterySOC.restart.max}
        step={validation.batterySOC.restart.step}
        unit="%"
      />
    </SettingSection>
  );
};

const styles = StyleSheet.create({
  subHeader: {
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 8,
    marginTop: 4,
  },
});

export default BatterySettingsSection;
