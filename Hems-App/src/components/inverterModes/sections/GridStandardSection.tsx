/**
 * GridStandardSection Component
 * API sortOrder: 3 - GridStandard (under SystemWorkMode)
 * User-level grid configuration settings
 */

import React from 'react';
import { StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useDeviceSettings } from '../../../hooks/useDeviceSettings';
import SettingSection from '../SettingSection';
import SettingToggle from '../SettingToggle';
import SettingPicker from '../SettingPicker';

interface GridStandardSectionProps {
  defaultExpanded?: boolean;
}

const GRID_STANDARD_OPTIONS = [
  { value: 'General', label: 'General' },
  { value: 'EN50549', label: 'EN 50549' },
  { value: 'VDE4105', label: 'VDE 4105' },
  { value: 'CEI021', label: 'CEI 0-21' },
  { value: 'G99', label: 'G99 UK' },
  { value: 'AS4777', label: 'AS 4777' },
];

const PHASE_TYPE_OPTIONS = [
  { value: 'ABC_0_120_240', label: 'ABC (0°/120°/240°)' },
  { value: 'ACB_0_240_120', label: 'ACB (0°/240°/120°)' },
];

const GRID_LEVEL_OPTIONS = [
  { value: 'LN:220VAC LL:380VAC', label: 'LN:220V / LL:380V' },
  { value: 'LN:230VAC LL:400VAC', label: 'LN:230V / LL:400V' },
  { value: 'LN:240VAC LL:415VAC', label: 'LN:240V / LL:415V' },
  { value: 'LN:240VAC LL:420VAC', label: 'LN:240V / LL:420V' },
  { value: 'LN:120VAC LL:208VAC', label: 'LN:120V / LL:208V' },
  { value: 'LN:133VAC LL:230VAC', label: 'LN:133V / LL:230V' },
];

const GRID_FREQUENCY_OPTIONS = [
  { value: '50', label: '50 Hz' },
  { value: '60', label: '60 Hz' },
];

const GridStandardSection: React.FC<GridStandardSectionProps> = ({
  defaultExpanded = false,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    settings,
    updateSetting,
    isLoading,
  } = useDeviceSettings();

  const gridStandard = settings.gridStandard;

  if (isLoading || !gridStandard) {
    return null;
  }

  return (
    <SettingSection
      title={t('inverterModes.settings.gridStandard', 'Grid Standard')}
      icon="globe"
      defaultExpanded={defaultExpanded}
    >
      <SettingPicker
        label={t('inverterModes.labels.gridStandard', 'Grid Standard')}
        value={gridStandard.standard}
        options={GRID_STANDARD_OPTIONS}
        onChange={(value) => updateSetting('gridStandard.standard', value)}
      />
      <SettingPicker
        label={t('inverterModes.labels.gridFrequency', 'Grid Frequency')}
        value={String(gridStandard.frequency)}
        options={GRID_FREQUENCY_OPTIONS}
        onChange={(value) => updateSetting('gridStandard.frequency', value)}
      />
      <SettingPicker
        label={t('inverterModes.labels.phaseType', 'Phase Sequence')}
        value={gridStandard.phaseType}
        options={PHASE_TYPE_OPTIONS}
        onChange={(value) => updateSetting('gridStandard.phaseType', value)}
      />
      <SettingPicker
        label={t('inverterModes.labels.gridLevel', 'Grid Voltage Level')}
        value={gridStandard.gridLevel}
        options={GRID_LEVEL_OPTIONS}
        onChange={(value) => updateSetting('gridStandard.gridLevel', value)}
      />
      <SettingToggle
        label={t('inverterModes.labels.itSystemNeutralGrounded', 'IT System Neutral Grounded')}
        value={gridStandard.itSystemNeutralGrounded}
        onChange={(value) => updateSetting('gridStandard.itSystemNeutralGrounded', value)}
        description={t('inverterModes.descriptions.itSystemNeutralGrounded', 'Enable neutral grounding for IT power systems')}
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

export default GridStandardSection;
