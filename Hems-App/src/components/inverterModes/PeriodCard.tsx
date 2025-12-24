/**
 * PeriodCard Component
 * Time of Use zaman dilimi kartı
 * API yapısına uyumlu - TimeSlot tipini kullanır
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Switch,
  Modal,
  ScrollView,
} from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { TimeSlot, TimeSlotVisibility, DEVICE_SETTINGS_VALIDATION } from '../../api/types/inverterModes.types';

interface PeriodCardProps {
  period: TimeSlot;
  index: number;
  onUpdate: (updates: Partial<TimeSlot>) => void;
  editable?: boolean;
  visibility?: TimeSlotVisibility;
}

// Time Picker Modal Component
const TimePickerModal: React.FC<{
  visible: boolean;
  value: string;
  onClose: () => void;
  onSelect: (time: string) => void;
  title: string;
}> = ({ visible, value, onClose, onSelect, title }) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const [hours, minutes] = value.split(':').map(Number);
  const [selectedHour, setSelectedHour] = useState(hours);
  const [selectedMinute, setSelectedMinute] = useState(minutes);

  const handleConfirm = () => {
    const time = `${selectedHour.toString().padStart(2, '0')}:${selectedMinute.toString().padStart(2, '0')}`;
    onSelect(time);
    onClose();
  };

  return (
    <Modal visible={visible} transparent animationType="slide">
      <View style={styles.modalOverlay}>
        <View style={[styles.modalContent, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <Text style={[styles.modalTitle, { color: COLORS.text }]}>{title}</Text>

          <View style={styles.pickerRow}>
            {/* Hour Picker */}
            <View style={styles.pickerColumn}>
              <Text style={[styles.pickerLabel, { color: COLORS.textSoft }]}>{t('timeOfUse.hour')}</Text>
              <ScrollView style={styles.pickerScroll} showsVerticalScrollIndicator={false}>
                {Array.from({ length: 24 }, (_, i) => (
                  <TouchableOpacity
                    key={i}
                    onPress={() => setSelectedHour(i)}
                    style={[
                      styles.pickerItem,
                      selectedHour === i && { backgroundColor: COLORS.accent },
                    ]}
                  >
                    <Text
                      style={[
                        styles.pickerItemText,
                        { color: selectedHour === i ? '#fff' : COLORS.text },
                      ]}
                    >
                      {i.toString().padStart(2, '0')}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>

            <Text style={[styles.pickerSeparator, { color: COLORS.text }]}>:</Text>

            {/* Minute Picker */}
            <View style={styles.pickerColumn}>
              <Text style={[styles.pickerLabel, { color: COLORS.textSoft }]}>{t('timeOfUse.minute')}</Text>
              <ScrollView style={styles.pickerScroll} showsVerticalScrollIndicator={false}>
                {Array.from({ length: 12 }, (_, i) => i * 5).map((m) => (
                  <TouchableOpacity
                    key={m}
                    onPress={() => setSelectedMinute(m)}
                    style={[
                      styles.pickerItem,
                      selectedMinute === m && { backgroundColor: COLORS.accent },
                    ]}
                  >
                    <Text
                      style={[
                        styles.pickerItemText,
                        { color: selectedMinute === m ? '#fff' : COLORS.text },
                      ]}
                    >
                      {m.toString().padStart(2, '0')}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>
          </View>

          <View style={styles.modalButtons}>
            <TouchableOpacity
              onPress={onClose}
              style={[styles.modalButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray200 }]}
            >
              <Text style={[styles.modalButtonText, { color: COLORS.text }]}>{t('inverterModes.cancel')}</Text>
            </TouchableOpacity>
            <TouchableOpacity
              onPress={handleConfirm}
              style={[styles.modalButton, { backgroundColor: COLORS.accent }]}
            >
              <Text style={[styles.modalButtonText, { color: '#fff' }]}>{t('timeOfUse.select')}</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

// Simple Slider with +/- buttons
const SimpleSlider: React.FC<{
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  step: number;
  unit: string;
  disabled?: boolean;
  color?: string;
}> = ({ label, value, onChange, min, max, step, unit, disabled = false, color }) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const accentColor = color || COLORS.accent;

  const progress = ((value - min) / (max - min)) * 100;

  return (
    <View style={styles.sliderSection}>
      <View style={styles.sliderHeader}>
        <Text style={[styles.settingLabel, { color: COLORS.text }]}>{label}</Text>
        <Text style={[styles.sliderValue, { color: accentColor }]}>
          {value}{unit}
        </Text>
      </View>
      <View style={[styles.progressBar, { backgroundColor: isDark ? '#333' : '#E0E0E0' }]}>
        <View style={[styles.progressFill, { width: `${progress}%`, backgroundColor: accentColor }]} />
      </View>
      <View style={styles.sliderControls}>
        <TouchableOpacity
          onPress={() => !disabled && onChange(Math.max(min, value - step))}
          disabled={disabled || value <= min}
          style={[styles.sliderButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray100, opacity: disabled || value <= min ? 0.4 : 1 }]}
        >
          <MaterialIcons name="remove" size={18} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.sliderRange, { color: COLORS.textSoft }]}>
          {min}{unit} - {max}{unit}
        </Text>
        <TouchableOpacity
          onPress={() => !disabled && onChange(Math.min(max, value + step))}
          disabled={disabled || value >= max}
          style={[styles.sliderButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray100, opacity: disabled || value >= max ? 0.4 : 1 }]}
        >
          <MaterialIcons name="add" size={18} color={COLORS.text} />
        </TouchableOpacity>
      </View>
    </View>
  );
};

// Default visibility (all visible)
const DEFAULT_VISIBILITY: TimeSlotVisibility = {
  startTime: true,
  endTime: true,
  gridCharge: true,
  genCharge: true,
  maxDischarge: true,
  minBatterySOC: true,
  minBatteryVoltage: true,
};

const PeriodCard: React.FC<PeriodCardProps> = ({
  period,
  index,
  onUpdate,
  editable = true,
  visibility = DEFAULT_VISIBILITY,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const [showStartPicker, setShowStartPicker] = useState(false);
  const [showEndPicker, setShowEndPicker] = useState(false);

  const validation = DEVICE_SETTINGS_VALIDATION;

  // Merge with defaults to ensure all keys exist
  const vis = { ...DEFAULT_VISIBILITY, ...visibility };

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.surface : COLORS.white },
      ]}
    >
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <View style={[styles.periodBadge, { backgroundColor: COLORS.accent }]}>
            <Text style={styles.periodBadgeText}>{period.id}</Text>
          </View>
          <Text style={[styles.title, { color: COLORS.text }]}>
            {t('timeOfUse.period')} {index + 1}
          </Text>
        </View>
        <Text style={[styles.timeRange, { color: COLORS.textSoft }]}>
          {period.startTime} - {period.endTime}
        </Text>
      </View>

      {/* Time Range */}
      {(vis.startTime || vis.endTime) && (
        <View style={styles.timeRow}>
          {vis.startTime && (
            <TouchableOpacity
              onPress={() => editable && setShowStartPicker(true)}
              style={[styles.timeButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray100 }]}
              disabled={!editable}
            >
              <Text style={[styles.timeLabel, { color: COLORS.textSoft }]}>{t('timeOfUse.startTime')}</Text>
              <Text style={[styles.timeValue, { color: COLORS.text }]}>{period.startTime}</Text>
            </TouchableOpacity>
          )}

          {vis.startTime && vis.endTime && (
            <MaterialIcons name="arrow-forward" size={20} color={COLORS.textSoft} />
          )}

          {vis.endTime && (
            <TouchableOpacity
              onPress={() => editable && setShowEndPicker(true)}
              style={[styles.timeButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray100 }]}
              disabled={!editable}
            >
              <Text style={[styles.timeLabel, { color: COLORS.textSoft }]}>{t('timeOfUse.endTime')}</Text>
              <Text style={[styles.timeValue, { color: COLORS.text }]}>{period.endTime}</Text>
            </TouchableOpacity>
          )}
        </View>
      )}

      {/* Grid Charge Toggle */}
      {vis.gridCharge && (
        <View style={styles.settingRow}>
          <Text style={[styles.settingLabel, { color: COLORS.text }]}>{t('timeOfUse.gridCharge')}</Text>
          <Switch
            value={period.gridCharge === 'on'}
            onValueChange={(value) => {
              if (editable) onUpdate({ gridCharge: value ? 'on' : 'off' });
            }}
            disabled={!editable}
            thumbColor={period.gridCharge === 'on' ? COLORS.accent : '#ccc'}
            trackColor={{ false: '#ccc', true: `${COLORS.accent}66` }}
          />
        </View>
      )}

      {/* Gen Charge Toggle */}
      {vis.genCharge && (
        <View style={styles.settingRow}>
          <Text style={[styles.settingLabel, { color: COLORS.text }]}>{t('timeOfUse.genCharge')}</Text>
          <Switch
            value={period.genCharge === 'on'}
            onValueChange={(value) => {
              if (editable) onUpdate({ genCharge: value ? 'on' : 'off' });
            }}
            disabled={!editable}
            thumbColor={period.genCharge === 'on' ? '#FF9800' : '#ccc'}
            trackColor={{ false: '#ccc', true: '#FF980066' }}
          />
        </View>
      )}

      {/* Min Battery SOC */}
      {vis.minBatterySOC && (
        <SimpleSlider
          label={t('timeOfUse.minBatterySOC', 'Min Battery SOC')}
          value={period.minBatterySOC}
          onChange={(value) => editable && onUpdate({ minBatterySOC: value })}
          min={0}
          max={100}
          step={5}
          unit="%"
          disabled={!editable}
        />
      )}

      {/* Max Discharge Power */}
      {vis.maxDischarge && (
        <SimpleSlider
          label={t('timeOfUse.maxDischargePower', 'Max Discharge')}
          value={period.maxDischarge}
          onChange={(value) => editable && onUpdate({ maxDischarge: value })}
          min={validation.power.maxDischarge.min}
          max={validation.power.maxDischarge.max}
          step={validation.power.maxDischarge.step}
          unit="W"
          disabled={!editable}
        />
      )}

      {/* Min Battery Voltage */}
      {vis.minBatteryVoltage && (
        <SimpleSlider
          label={t('timeOfUse.minBatteryVoltage', 'Min Battery Voltage')}
          value={period.minBatteryVoltage}
          onChange={(value) => editable && onUpdate({ minBatteryVoltage: value })}
          min={validation.voltage.minBattery.min}
          max={validation.voltage.minBattery.max}
          step={validation.voltage.minBattery.step}
          unit="V"
          disabled={!editable}
          color="#9C27B0"
        />
      )}

      {/* Time Picker Modals */}
      <TimePickerModal
        visible={showStartPicker}
        value={period.startTime}
        onClose={() => setShowStartPicker(false)}
        onSelect={(time) => onUpdate({ startTime: time })}
        title={t('timeOfUse.startTimeTitle')}
      />
      <TimePickerModal
        visible={showEndPicker}
        value={period.endTime}
        onClose={() => setShowEndPicker(false)}
        onSelect={(time) => onUpdate({ endTime: time })}
        title={t('timeOfUse.endTimeTitle')}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  periodBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 4,
  },
  periodBadgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '700',
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
  },
  timeRange: {
    fontSize: 12,
    fontWeight: '500',
  },
  timeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 16,
    gap: 8,
  },
  timeButton: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  timeLabel: {
    fontSize: 11,
    marginBottom: 2,
  },
  timeValue: {
    fontSize: 18,
    fontWeight: '700',
  },
  settingRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
  },
  settingLabel: {
    fontSize: 14,
    fontWeight: '500',
  },
  sliderSection: {
    marginTop: 12,
  },
  sliderHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  sliderValue: {
    fontSize: 14,
    fontWeight: '700',
  },
  progressBar: {
    height: 6,
    borderRadius: 3,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: 3,
  },
  sliderControls: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  sliderButton: {
    width: 36,
    height: 36,
    borderRadius: 18,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sliderRange: {
    fontSize: 11,
  },
  // Modal Styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    textAlign: 'center',
    marginBottom: 20,
  },
  pickerRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 20,
  },
  pickerColumn: {
    alignItems: 'center',
  },
  pickerLabel: {
    fontSize: 12,
    marginBottom: 8,
  },
  pickerScroll: {
    height: 150,
    width: 60,
  },
  pickerItem: {
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
    marginVertical: 2,
  },
  pickerItemText: {
    fontSize: 18,
    fontWeight: '600',
    textAlign: 'center',
  },
  pickerSeparator: {
    fontSize: 24,
    fontWeight: '700',
    marginHorizontal: 16,
  },
  modalButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  modalButton: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
  },
  modalButtonText: {
    fontSize: 16,
    fontWeight: '600',
  },
});

export default PeriodCard;
