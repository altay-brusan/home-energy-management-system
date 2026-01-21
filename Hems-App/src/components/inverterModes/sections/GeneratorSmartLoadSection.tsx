/**
 * GeneratorSmartLoadSection Component
 * API sortOrder: 4 - GeneratorAndSmartLoad kategorisi
 * Contains: GeneratorPeakShaving, SmartLoad
 * Note: Generator charging settings are in BatterySettings > GeneratorCharging section
 */

import React, { useMemo } from 'react';
import { Text, StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useGeneratorSmartLoadSection } from '../../../hooks/useDeviceSettings';
import { DEVICE_SETTINGS_VALIDATION, SmartLoadMode } from '../../../api/types/inverterModes.types';
import SettingSection from '../SettingSection';
import SettingToggle from '../SettingToggle';
import SettingSlider from '../SettingSlider';
import SettingPicker from '../SettingPicker';

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
    generatorPeakShaving,
    smartLoad,
    updateGeneratorPeakShaving,
    updateSmartLoad,
    isLoading,
  } = useGeneratorSmartLoadSection();

  const validation = DEVICE_SETTINGS_VALIDATION;

  // Smart Load Mode options (0=Generator, 1=SmartLoad, 2=Microinverter)
  const smartLoadModeOptions = useMemo(() => [
    { value: '0', label: t('inverterModes.smartLoadModes.generator', 'Generator Only') },
    { value: '1', label: t('inverterModes.smartLoadModes.smartLoad', 'Smart Load') },
    { value: '2', label: t('inverterModes.smartLoadModes.microinverter', 'Microinverter') },
  ], [t]);

  // Check if Smart Load mode is active (mode 1)
  const isSmartLoadActive = smartLoad.enabled === 1;

  if (isLoading) {
    return null;
  }

  return (
    <SettingSection
      title={t('inverterModes.settings.generatorSmartLoad', 'Generator & Smart Load')}
      icon="bolt"
      defaultExpanded={defaultExpanded}
    >
      {/* Generator Peak Shaving */}
      <Text style={[styles.subHeader, { color: COLORS.text }]}>
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
      <SettingPicker
        label={t('inverterModes.labels.smartLoadMode', 'Generator Port Mode')}
        value={String(smartLoad.enabled)}
        options={smartLoadModeOptions}
        onChange={(value) => updateSmartLoad({ enabled: Number(value) as SmartLoadMode })}
      />
      {isSmartLoadActive && (
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
