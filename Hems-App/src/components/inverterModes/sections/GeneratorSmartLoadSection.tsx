/**
 * GeneratorSmartLoadSection Component
 * API sortOrder: 4 - GeneratorAndSmartLoad kategorisi
 * Tek bir section içinde: GeneratorSettings, GeneratorPeakShaving, SmartLoad
 */

import React from 'react';
import { Text, StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useGeneratorSmartLoadSection } from '../../../hooks/useDeviceSettings';
import { DEVICE_SETTINGS_VALIDATION } from '../../../api/types/inverterModes.types';
import SettingSection from '../SettingSection';
import SettingToggle from '../SettingToggle';
import SettingSlider from '../SettingSlider';

interface GeneratorSmartLoadSectionProps {
  defaultExpanded?: boolean;
}

const GeneratorSmartLoadSection: React.FC<GeneratorSmartLoadSectionProps> = ({
  defaultExpanded = false,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    generatorSettings,
    generatorPeakShaving,
    smartLoad,
    updateGeneratorSettings,
    updateGeneratorPeakShaving,
    updateSmartLoad,
    isLoading,
  } = useGeneratorSmartLoadSection();

  const validation = DEVICE_SETTINGS_VALIDATION;

  if (isLoading) {
    return null;
  }

  return (
    <SettingSection
      title={t('inverterModes.settings.generatorSmartLoad', 'Generator & Smart Load')}
      icon="bolt"
      defaultExpanded={defaultExpanded}
    >
      {/* Generator Settings */}
      <Text style={[styles.subHeader, { color: COLORS.text }]}>
        {t('inverterModes.settings.generatorSettings', 'Generator Settings')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.generatorCharge', 'Generator Charge')}
        value={generatorSettings.charge}
        onChange={(value) => updateGeneratorSettings({ charge: value })}
        description={t('inverterModes.descriptions.generatorCharge', 'Allow generator to charge battery')}
      />
      <SettingToggle
        label={t('inverterModes.labels.generatorGridCharge', 'Generator Grid Charge')}
        value={generatorSettings.gridCharge}
        onChange={(value) => updateGeneratorSettings({ gridCharge: value })}
        description={t('inverterModes.descriptions.generatorGridCharge', 'Allow generator to charge when grid is available')}
      />
      {generatorSettings.charge === 'on' && (
        <>
          <SettingSlider
            label={t('inverterModes.labels.generatorStartSOC', 'Start SOC')}
            value={generatorSettings.startSOC}
            onChange={(value) => updateGeneratorSettings({ startSOC: value })}
            min={validation.batterySOC.generatorStart.min}
            max={validation.batterySOC.generatorStart.max}
            step={validation.batterySOC.generatorStart.step}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.generatorChargeCurrent', 'Charge Current')}
            value={generatorSettings.chargeCurrent}
            onChange={(value) => updateGeneratorSettings({ chargeCurrent: value })}
            min={validation.current.genCharge.min}
            max={validation.current.genCharge.max}
            step={validation.current.genCharge.step}
            unit="A"
          />
        </>
      )}

      {/* Generator Peak Shaving */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.generatorPeakShaving', 'Generator Peak Shaving')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.generatorPeakShavingEnabled', 'Enable Peak Shaving')}
        value={generatorPeakShaving.enabled}
        onChange={(value) => updateGeneratorPeakShaving({ enabled: value })}
        description={t('inverterModes.descriptions.generatorPeakShaving', 'Limit power from generator during peak times')}
      />
      {generatorPeakShaving.enabled === 'on' && (
        <SettingSlider
          label={t('inverterModes.labels.generatorPeakShavingLimitPower', 'Limit Power')}
          value={generatorPeakShaving.limitPower}
          onChange={(value) => updateGeneratorPeakShaving({ limitPower: value })}
          min={validation.power.genPeakShaving.min}
          max={validation.power.genPeakShaving.max}
          step={validation.power.genPeakShaving.step}
          unit="W"
        />
      )}

      {/* Smart Load */}
      <Text style={[styles.subHeader, { color: COLORS.text, marginTop: 16 }]}>
        {t('inverterModes.settings.smartLoad', 'Smart Load')}
      </Text>
      <Text style={[styles.infoText, { color: COLORS.textSoft }]}>
        {t('inverterModes.descriptions.smartLoad', 'Configure smart load behavior based on battery and PV conditions')}
      </Text>
      <SettingToggle
        label={t('inverterModes.labels.smartLoadEnabled', 'Enable Smart Load')}
        value={smartLoad.enabled}
        onChange={(value) => updateSmartLoad({ enabled: value })}
      />
      {smartLoad.enabled === 'on' && (
        <>
          <SettingSlider
            label={t('inverterModes.labels.smartLoadOffSOC', 'Turn Off SOC')}
            value={smartLoad.offSOC}
            onChange={(value) => updateSmartLoad({ offSOC: value })}
            min={0}
            max={100}
            step={1}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.smartLoadOnSOC', 'Turn On SOC')}
            value={smartLoad.onSOC}
            onChange={(value) => updateSmartLoad({ onSOC: value })}
            min={0}
            max={100}
            step={1}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.smartLoadPVThreshold', 'PV Power Threshold')}
            value={smartLoad.pvPowerThreshold}
            onChange={(value) => updateSmartLoad({ pvPowerThreshold: value })}
            min={validation.power.smartLoadPVThreshold.min}
            max={validation.power.smartLoadPVThreshold.max}
            step={validation.power.smartLoadPVThreshold.step}
            unit="W"
          />
          <SettingSlider
            label={t('inverterModes.labels.smartLoadOpenDelay', 'Open Delay')}
            value={smartLoad.openDelay}
            onChange={(value) => updateSmartLoad({ openDelay: value })}
            min={validation.time.smartLoadOpenDelay.min}
            max={validation.time.smartLoadOpenDelay.max}
            step={validation.time.smartLoadOpenDelay.step}
            unit={t('inverterModes.minutes', 'min')}
          />
          <SettingToggle
            label={t('inverterModes.labels.smartLoadOnGridAlwaysOn', 'On-Grid Always On')}
            value={smartLoad.onGridAlwaysOn}
            onChange={(value) => updateSmartLoad({ onGridAlwaysOn: value })}
            description={t('inverterModes.descriptions.smartLoadOnGridAlwaysOn', 'Keep smart load always on when grid is available')}
          />
        </>
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
  infoText: {
    fontSize: 13,
    lineHeight: 18,
    marginBottom: 12,
  },
});

export default GeneratorSmartLoadSection;
