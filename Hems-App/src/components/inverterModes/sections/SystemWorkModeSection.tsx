/**
 * SystemWorkModeSection Component
 * API sortOrder: 2 - SystemWorkMode kategorisi
 * Tek bir section içinde: ExportStrategy, EnergyPriority, InverterMode, GridPeakShaving
 */

import React from 'react';
import { Text, StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useSystemWorkModeSection } from '../../../hooks/useDeviceSettings';
import { DEVICE_SETTINGS_VALIDATION, WorkModeType, EnergyPatternType } from '../../../api/types/inverterModes.types';
import SettingSection from '../SettingSection';
import SettingToggle from '../SettingToggle';
import SettingSlider from '../SettingSlider';
import SettingPicker from '../SettingPicker';

interface SystemWorkModeSectionProps {
  defaultExpanded?: boolean;
}

const EXPORT_STRATEGY_OPTIONS = [
  { value: 'selling_first', label: 'Selling First' },
  { value: 'zero_export_to_load', label: 'Zero Export to Load' },
  { value: 'zero_export_to_ct', label: 'Zero Export to CT' },
];

const ENERGY_PRIORITY_OPTIONS = [
  { value: 'batt_first', label: 'Battery First' },
  { value: 'load_first', label: 'Load First' },
];

const SystemWorkModeSection: React.FC<SystemWorkModeSectionProps> = ({
  defaultExpanded = true,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    exportStrategy,
    energyPriority,
    solarSell,
    maxSellPower,
    zeroExportLimit,
    forceOffGrid,
    gridPeakShavingEnabled,
    gridPeakShavingLimitPower,
    updateField,
    isLoading,
  } = useSystemWorkModeSection();

  const validation = DEVICE_SETTINGS_VALIDATION;

  if (isLoading) {
    return null;
  }

  return (
    <SettingSection
      title={t('inverterModes.settings.systemWorkMode', 'System Work Mode')}
      icon="cog"
      defaultExpanded={defaultExpanded}
    >
      {/* Export Strategy & Energy Priority */}
      <Text style={[styles.subHeader, { color: COLORS.text }]}>
        {t('inverterModes.settings.workModeStrategy', 'Work Mode Strategy')}
      </Text>
      <SettingPicker
        label={t('inverterModes.labels.exportStrategy', 'Export Strategy')}
        value={exportStrategy}
        options={EXPORT_STRATEGY_OPTIONS}
        onChange={(value) => updateField('exportStrategy', value as WorkModeType)}
      />
      <SettingPicker
        label={t('inverterModes.labels.energyPriority', 'Energy Priority')}
        value={energyPriority}
        options={ENERGY_PRIORITY_OPTIONS}
        onChange={(value) => updateField('energyPriority', value as EnergyPatternType)}
      />

      {/* Inverter Mode Settings */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.inverterMode', 'Inverter Mode')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.solarSell', 'Solar Sell')}
        value={solarSell}
        onChange={(value) => updateField('solarSell', value)}
        description={t('inverterModes.descriptions.solarSell', 'Sell excess solar power to the grid')}
      />
      {solarSell === 'on' && (
        <SettingSlider
          label={t('inverterModes.labels.maxSellPower', 'Max Sell Power')}
          value={maxSellPower}
          onChange={(value) => updateField('maxSellPower', value)}
          min={validation.power.maxSellPower.min}
          max={validation.power.maxSellPower.max}
          step={validation.power.maxSellPower.step}
          unit="W"
        />
      )}
      <SettingSlider
        label={t('inverterModes.labels.zeroExportLimit', 'Zero Export Limit')}
        value={zeroExportLimit}
        onChange={(value) => updateField('zeroExportLimit', value)}
        min={validation.power.zeroExportLimit.min}
        max={validation.power.zeroExportLimit.max}
        step={validation.power.zeroExportLimit.step}
        unit="W"
      />
      <SettingToggle
        label={t('inverterModes.labels.forceOffGrid', 'Force Off-Grid')}
        value={forceOffGrid}
        onChange={(value) => updateField('forceOffGrid', value)}
        description={t('inverterModes.descriptions.forceOffGrid', 'Force the system to operate in off-grid mode')}
      />

      {/* Grid Peak Shaving */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.gridPeakShaving', 'Grid Peak Shaving')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.gridPeakShavingEnabled', 'Enable Peak Shaving')}
        value={gridPeakShavingEnabled}
        onChange={(value) => updateField('gridPeakShavingEnabled', value)}
        description={t('inverterModes.descriptions.gridPeakShaving', 'Limit power drawn from grid during peak times')}
      />
      {gridPeakShavingEnabled === 'on' && (
        <SettingSlider
          label={t('inverterModes.labels.gridPeakShavingLimitPower', 'Limit Power')}
          value={gridPeakShavingLimitPower}
          onChange={(value) => updateField('gridPeakShavingLimitPower', value)}
          min={validation.power.gridPeakShaving.min}
          max={validation.power.gridPeakShaving.max}
          step={validation.power.gridPeakShaving.step}
          unit="W"
        />
      )}
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

export default SystemWorkModeSection;
