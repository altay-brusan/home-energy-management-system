/**
 * MaintenanceSettingsScreen
 * Admin-only maintenance settings ekranı
 * API sortOrder: 5 - MaintenanceSettings
 */

import React, { useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Shield, AlertTriangle } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useDeviceSettings } from '../../../hooks/useDeviceSettings';
import { useRoles } from '../../../hooks/useRoles';
import {
  SettingSection,
  SettingToggle,
  SettingSlider,
  SettingPicker,
} from '../../../components/inverterModes';

const MaintenanceSettingsScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { isAdmin } = useRoles();

  const {
    maintenanceSettings,
    isLoading,
    updateSetting,
  } = useDeviceSettings();

  // Redirect if not admin
  useEffect(() => {
    if (!isAdmin) {
      Alert.alert(
        t('common.accessDenied', 'Access Denied'),
        t('inverterModes.adminOnlyMessage', 'This section is only accessible to administrators.'),
        [{ text: t('common.ok', 'OK'), onPress: () => navigation.goBack() }]
      );
    }
  }, [isAdmin, navigation, t]);

  if (!isAdmin) {
    return null;
  }

  // Loading state
  if (isLoading || !maintenanceSettings) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
        <ActivityIndicator size="large" color={COLORS.accent} />
      </View>
    );
  }

  const { batteryMaintenance, gridCode, advanced, externalRelay, generatorOverride } = maintenanceSettings;

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
        <View style={styles.titleContainer}>
          <Shield size={20} color={COLORS.warning} />
          <Text style={[styles.title, { color: COLORS.text }]}>
            {t('inverterModes.maintenanceSettings', 'Maintenance Settings')}
          </Text>
        </View>
        <View style={{ width: 40 }} />
      </View>

      {/* Warning Banner */}
      <View style={[styles.warningBanner, { backgroundColor: `${COLORS.warning}20` }]}>
        <AlertTriangle size={18} color={COLORS.warning} />
        <Text style={[styles.warningText, { color: COLORS.warning }]}>
          {t('inverterModes.maintenanceWarning', 'These settings are for advanced users. Incorrect configuration may affect system operation.')}
        </Text>
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 20 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Battery Maintenance */}
        <SettingSection
          title={t('inverterModes.settings.batteryMaintenance', 'Battery Maintenance')}
          icon="battery-full"
          defaultExpanded
        >
          <SettingPicker
            label={t('inverterModes.labels.batteryControlMode', 'Control Mode')}
            value={batteryMaintenance.controlMode}
            options={[
              { value: 'Voltage', label: 'Voltage Mode' },
              { value: 'SOC', label: 'SOC Mode' },
              { value: 'Hybrid', label: 'Hybrid Mode' },
            ]}
            onChange={(value) => updateSetting('maintenanceSettings.batteryMaintenance.controlMode', value)}
          />
          <SettingToggle
            label={t('inverterModes.labels.lithiumWakeUp', 'Lithium Battery Wake Up')}
            value={batteryMaintenance.lithiumWakeUpEnabled}
            onChange={(value) => updateSetting('maintenanceSettings.batteryMaintenance.lithiumWakeUpEnabled', value)}
            description={t('inverterModes.descriptions.lithiumWakeUp', 'Enable automatic lithium battery wake-up function')}
          />
          <SettingSlider
            label={t('inverterModes.labels.batteryResistance', 'Battery Resistance')}
            value={batteryMaintenance.resistance}
            onChange={(value) => updateSetting('maintenanceSettings.batteryMaintenance.resistance', value)}
            min={0}
            max={100}
            step={1}
            unit="mΩ"
          />
          <SettingSlider
            label={t('inverterModes.labels.chargeEfficiency', 'Charge Efficiency')}
            value={batteryMaintenance.chargeEfficiency}
            onChange={(value) => updateSetting('maintenanceSettings.batteryMaintenance.chargeEfficiency', value)}
            min={80}
            max={100}
            step={1}
            unit="%"
          />
        </SettingSection>

        {/* Grid Protection Parameters */}
        <SettingSection
          title={t('inverterModes.settings.gridProtection', 'Grid Protection Parameters')}
          icon="plug"
        >
          <SettingSlider
            label={t('inverterModes.labels.voltageHigh', 'Voltage High Limit')}
            value={gridCode.voltageHigh}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.voltageHigh', value)}
            min={240}
            max={280}
            step={1}
            unit="V"
          />
          <SettingSlider
            label={t('inverterModes.labels.voltageLow', 'Voltage Low Limit')}
            value={gridCode.voltageLow}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.voltageLow', value)}
            min={180}
            max={220}
            step={1}
            unit="V"
          />
          <SettingSlider
            label={t('inverterModes.labels.frequencyHigh', 'Frequency High Limit')}
            value={gridCode.frequencyHigh}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.frequencyHigh', value)}
            min={50}
            max={55}
            step={0.1}
            unit="Hz"
          />
          <SettingSlider
            label={t('inverterModes.labels.frequencyLow', 'Frequency Low Limit')}
            value={gridCode.frequencyLow}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.frequencyLow', value)}
            min={45}
            max={50}
            step={0.1}
            unit="Hz"
          />
          <SettingSlider
            label={t('inverterModes.labels.reconnectionTime', 'Reconnection Time')}
            value={gridCode.reconnectionTime}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.reconnectionTime', value)}
            min={0}
            max={600}
            step={10}
            unit="s"
          />
        </SettingSection>

        {/* Power Factor Settings */}
        <SettingSection
          title={t('inverterModes.settings.powerFactor', 'Power Factor Settings')}
          icon="activity"
        >
          <SettingSlider
            label={t('inverterModes.labels.powerFactorTarget', 'Power Factor Target')}
            value={gridCode.powerFactorTarget}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.powerFactorTarget', value)}
            min={0.8}
            max={1.0}
            step={0.01}
            unit=""
          />
          <SettingSlider
            label={t('inverterModes.labels.activePowerRegulation', 'Active Power Regulation')}
            value={gridCode.activePowerRegulation}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.activePowerRegulation', value)}
            min={0}
            max={100}
            step={1}
            unit="%"
          />
          <SettingSlider
            label={t('inverterModes.labels.reactivePowerRegulation', 'Reactive Power Regulation')}
            value={gridCode.reactivePowerRegulation}
            onChange={(value) => updateSetting('maintenanceSettings.gridCode.reactivePowerRegulation', value)}
            min={0}
            max={100}
            step={1}
            unit="%"
          />
        </SettingSection>

        {/* Advanced Functions */}
        <SettingSection
          title={t('inverterModes.settings.advanced', 'Advanced Functions')}
          icon="sliders"
        >
          <SettingToggle
            label={t('inverterModes.labels.arcFaultDetection', 'Arc Fault Detection')}
            value={advanced.arcFaultDetection}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.arcFaultDetection', value)}
            description={t('inverterModes.descriptions.arcFaultDetection', 'Enable AFCI (Arc Fault Circuit Interrupter)')}
          />
          <SettingToggle
            label={t('inverterModes.labels.islandProtection', 'Island Protection')}
            value={advanced.islandProtection}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.islandProtection', value)}
            description={t('inverterModes.descriptions.islandProtection', 'Anti-islanding protection for grid safety')}
          />
          <SettingToggle
            label={t('inverterModes.labels.lithiumBatteryLossFault', 'Lithium Battery Loss Fault')}
            value={advanced.lithiumBatteryLossFaultEnabled}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.lithiumBatteryLossFaultEnabled', value)}
            description={t('inverterModes.descriptions.lithiumBatteryLossFault', 'Enable lithium battery communication loss fault')}
          />
        </SettingSection>

        {/* Parallel Settings */}
        <SettingSection
          title={t('inverterModes.settings.parallel', 'Parallel Settings')}
          icon="git-merge"
        >
          <SettingToggle
            label={t('inverterModes.labels.parallelEnabled', 'Parallel Operation')}
            value={advanced.parallelEnabled}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.parallelEnabled', value)}
            description={t('inverterModes.descriptions.parallelEnabled', 'Enable parallel inverter operation')}
          />
          {advanced.parallelEnabled === 'on' && (
            <>
              <SettingPicker
                label={t('inverterModes.labels.parallelRole', 'Parallel Role')}
                value={advanced.parallelRole}
                options={[
                  { value: 'standalone', label: 'Standalone' },
                  { value: 'Master', label: 'Master' },
                  { value: 'Slave', label: 'Slave' },
                ]}
                onChange={(value) => updateSetting('maintenanceSettings.advanced.parallelRole', value)}
              />
              <SettingPicker
                label={t('inverterModes.labels.parallelPhase', 'Parallel Phase')}
                value={advanced.parallelPhase}
                options={[
                  { value: 'A', label: 'Phase A' },
                  { value: 'B', label: 'Phase B' },
                  { value: 'C', label: 'Phase C' },
                ]}
                onChange={(value) => updateSetting('maintenanceSettings.advanced.parallelPhase', value)}
              />
              <SettingSlider
                label={t('inverterModes.labels.modbusAddress', 'Modbus Address')}
                value={advanced.modbusAddress}
                onChange={(value) => updateSetting('maintenanceSettings.advanced.modbusAddress', value)}
                min={0}
                max={63}
                step={1}
                unit=""
              />
            </>
          )}
        </SettingSection>

        {/* CT Settings */}
        <SettingSection
          title={t('inverterModes.settings.ctSettings', 'CT Settings')}
          icon="radio"
        >
          <SettingSlider
            label={t('inverterModes.labels.externalCTRatio', 'External CT Ratio')}
            value={advanced.externalCTRatio}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.externalCTRatio', value)}
            min={1}
            max={5000}
            step={1}
            unit=":1"
          />
          <SettingSlider
            label={t('inverterModes.labels.externalMeterCTRatio', 'External Meter CT Ratio')}
            value={advanced.externalMeterCTRatio}
            onChange={(value) => updateSetting('maintenanceSettings.advanced.externalMeterCTRatio', value)}
            min={1}
            max={100}
            step={1}
            unit=":1"
          />
        </SettingSection>

        {/* External Relay Configuration */}
        <SettingSection
          title={t('inverterModes.settings.externalRelay', 'External Relay Configuration')}
          icon="toggle-right"
        >
          <SettingToggle
            label={t('inverterModes.labels.externalRelayEnabled', 'External Relay Feature')}
            value={externalRelay?.enabled || 'off'}
            onChange={(value) => updateSetting('maintenanceSettings.externalRelay.enabled', value)}
            description={t('inverterModes.descriptions.externalRelayEnabled', 'Enable external relay control feature')}
          />
          {externalRelay?.enabled === 'on' && (
            <SettingSlider
              label={t('inverterModes.labels.relayBitmap', 'Relay Bitmap')}
              value={externalRelay?.bitmap || 0}
              onChange={(value) => updateSetting('maintenanceSettings.externalRelay.bitmap', value)}
              min={0}
              max={65535}
              step={1}
              unit=""
            />
          )}
        </SettingSection>

        {/* Generator Override */}
        <SettingSection
          title={t('inverterModes.settings.generatorOverride', 'Generator Override')}
          icon="zap"
        >
          <SettingToggle
            label={t('inverterModes.labels.forceSmartLoadOn', 'Force Smart Load On')}
            value={generatorOverride?.forceSmartLoadOn || 'off'}
            onChange={(value) => updateSetting('maintenanceSettings.generatorOverride.forceSmartLoadOn', value)}
            description={t('inverterModes.descriptions.forceSmartLoadOn', 'Override smart load settings to force it on')}
          />
        </SettingSection>

        {/* Info Box */}
        <View
          style={[
            styles.infoBox,
            { backgroundColor: isDark ? `${COLORS.warning}15` : '#FFF3E0' },
          ]}
        >
          <Text style={[styles.infoTitle, { color: COLORS.text }]}>
            {t('inverterModes.maintenanceInfo.title', 'Important')}
          </Text>
          <Text style={[styles.infoText, { color: COLORS.textSoft }]}>
            {t('inverterModes.maintenanceInfo.body', 'Changes to these settings may require a system restart. Some parameters are region-specific and should comply with local regulations.')}
          </Text>
        </View>
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
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
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
  },
  warningBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 12,
    marginHorizontal: 16,
    marginBottom: 12,
    borderRadius: 8,
    gap: 10,
  },
  warningText: {
    flex: 1,
    fontSize: 12,
    lineHeight: 16,
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  infoBox: {
    borderRadius: 12,
    padding: 16,
    marginTop: 8,
  },
  infoTitle: {
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 8,
  },
  infoText: {
    fontSize: 13,
    lineHeight: 18,
  },
});

export default MaintenanceSettingsScreen;
