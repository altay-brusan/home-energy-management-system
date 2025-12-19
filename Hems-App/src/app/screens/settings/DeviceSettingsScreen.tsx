import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, Modal, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Calendar, Clock, RotateCcw, ChevronRight, ChevronUp, ChevronDown } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makeDeviceSettingsScreenStyles } from './DeviceSettingsScreen.styles';
import { toast } from '../../../utils/toast';

interface NumberPickerProps {
  value: number;
  min: number;
  max: number;
  onChange: (value: number) => void;
  label: string;
  colors: Record<string, string>;
  isDark: boolean;
  padZero?: boolean;
}

const NumberPicker: React.FC<NumberPickerProps> = ({
  value,
  min,
  max,
  onChange,
  label,
  colors,
  isDark,
  padZero = true,
}) => {
  const increment = () => {
    if (value < max) onChange(value + 1);
    else onChange(min);
  };

  const decrement = () => {
    if (value > min) onChange(value - 1);
    else onChange(max);
  };

  const displayValue = padZero ? value.toString().padStart(2, '0') : value.toString();

  return (
    <View style={{ alignItems: 'center', marginHorizontal: 8 }}>
      <Text style={{ color: colors.textSoft, fontSize: 12, marginBottom: 8 }}>{label}</Text>
      <TouchableOpacity onPress={increment} style={{ padding: 8 }}>
        <ChevronUp size={24} color={colors.accent} />
      </TouchableOpacity>
      <View
        style={{
          backgroundColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.05)',
          borderRadius: 8,
          paddingVertical: 12,
          paddingHorizontal: 16,
          minWidth: 60,
          alignItems: 'center',
        }}
      >
        <Text style={{ color: colors.text, fontSize: 24, fontWeight: '600' }}>{displayValue}</Text>
      </View>
      <TouchableOpacity onPress={decrement} style={{ padding: 8 }}>
        <ChevronDown size={24} color={colors.accent} />
      </TouchableOpacity>
    </View>
  );
};

export default function DeviceSettingsScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeDeviceSettingsScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  const now = new Date();
  const [day, setDay] = useState(now.getDate());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());
  const [hour, setHour] = useState(now.getHours());
  const [minute, setMinute] = useState(now.getMinutes());
  const [isApplying, setIsApplying] = useState(false);
  const [showDateModal, setShowDateModal] = useState(false);
  const [showTimeModal, setShowTimeModal] = useState(false);

  const getDaysInMonth = (m: number, y: number) => {
    return new Date(y, m, 0).getDate();
  };

  const handleApplyDateTime = async () => {
    setIsApplying(true);
    try {
      const selectedDate = new Date(year, month - 1, day, hour, minute);
      // TODO: Call API to set device date/time
      // await deviceSettingsService.setDateTime(selectedDate);
      toast.success(t('settings.dateTimeSet'), t('common.success'));
    } catch (error) {
      toast.error(t('settings.dateTimeSetError'), t('common.error'));
    } finally {
      setIsApplying(false);
    }
  };

  const formatDate = () => {
    return `${day.toString().padStart(2, '0')}/${month.toString().padStart(2, '0')}/${year}`;
  };

  const formatTime = () => {
    return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
  };

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>{t('settings.deviceSettings')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Date & Time Section */}
        <View style={[styles.sectionCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <View style={styles.sectionHeader}>
            <View style={[styles.iconContainer, { backgroundColor: `${COLORS.accent}15` }]}>
              <Calendar size={20} color={COLORS.accent} />
            </View>
            <View style={styles.sectionTitleContainer}>
              <Text style={[styles.sectionTitle, { color: COLORS.text }]}>
                {t('settings.setDateTime')}
              </Text>
              <Text style={[styles.sectionSubtitle, { color: COLORS.textSoft }]}>
                {t('settings.setDateTimeDesc')}
              </Text>
            </View>
          </View>

          {/* Date Selector */}
          <TouchableOpacity
            style={[styles.dateTimeRow, { borderTopColor: isDark ? COLORS.border : COLORS.gray200 }]}
            onPress={() => setShowDateModal(true)}
          >
            <Text style={[styles.dateTimeLabel, { color: COLORS.text }]}>
              {t('settings.date')}
            </Text>
            <View style={styles.dateTimeValue}>
              <Text style={[styles.dateTimeValueText, { color: COLORS.accent }]}>
                {formatDate()}
              </Text>
              <ChevronRight size={18} color={COLORS.textSoft} />
            </View>
          </TouchableOpacity>

          {/* Time Selector */}
          <TouchableOpacity
            style={[styles.dateTimeRow, { borderTopColor: isDark ? COLORS.border : COLORS.gray200 }]}
            onPress={() => setShowTimeModal(true)}
          >
            <Text style={[styles.dateTimeLabel, { color: COLORS.text }]}>
              {t('settings.time')}
            </Text>
            <View style={styles.dateTimeValue}>
              <Clock size={16} color={COLORS.textSoft} style={{ marginRight: 6 }} />
              <Text style={[styles.dateTimeValueText, { color: COLORS.accent }]}>
                {formatTime()}
              </Text>
              <ChevronRight size={18} color={COLORS.textSoft} />
            </View>
          </TouchableOpacity>

          {/* Apply Button */}
          <TouchableOpacity
            style={[
              styles.applyButton,
              { backgroundColor: COLORS.accent },
              isApplying && styles.applyButtonDisabled,
            ]}
            onPress={handleApplyDateTime}
            disabled={isApplying}
          >
            <Text style={styles.applyButtonText}>
              {isApplying ? t('energyProfile.applyingSettings') : t('common.save')}
            </Text>
          </TouchableOpacity>
        </View>

        {/* Reset Options Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={() => navigation.navigate('ResetSettings')}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.warning}15` }]}>
            <RotateCcw size={22} color={COLORS.warning} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.resetOptions')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.resetOptionsDesc')}
            </Text>
          </View>
          <ChevronRight size={20} color={COLORS.textSoft} />
        </TouchableOpacity>
      </ScrollView>

      {/* Date Picker Modal */}
      <Modal
        visible={showDateModal}
        transparent
        animationType="fade"
        onRequestClose={() => setShowDateModal(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setShowDateModal(false)}
        >
          <View
            style={[styles.modalContent, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
            onStartShouldSetResponder={() => true}
          >
            <Text style={[styles.modalTitle, { color: COLORS.text }]}>
              {t('settings.date')}
            </Text>
            <View style={styles.pickerRow}>
              <NumberPicker
                value={day}
                min={1}
                max={getDaysInMonth(month, year)}
                onChange={setDay}
                label={t('settings.day')}
                colors={COLORS}
                isDark={isDark}
              />
              <NumberPicker
                value={month}
                min={1}
                max={12}
                onChange={setMonth}
                label={t('settings.month')}
                colors={COLORS}
                isDark={isDark}
              />
              <NumberPicker
                value={year}
                min={2020}
                max={2030}
                onChange={setYear}
                label={t('settings.year')}
                colors={COLORS}
                isDark={isDark}
                padZero={false}
              />
            </View>
            <TouchableOpacity
              style={[styles.modalButton, { backgroundColor: COLORS.accent }]}
              onPress={() => setShowDateModal(false)}
            >
              <Text style={styles.modalButtonText}>{t('common.confirm')}</Text>
            </TouchableOpacity>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Time Picker Modal */}
      <Modal
        visible={showTimeModal}
        transparent
        animationType="fade"
        onRequestClose={() => setShowTimeModal(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setShowTimeModal(false)}
        >
          <View
            style={[styles.modalContent, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
            onStartShouldSetResponder={() => true}
          >
            <Text style={[styles.modalTitle, { color: COLORS.text }]}>
              {t('settings.time')}
            </Text>
            <View style={styles.pickerRow}>
              <NumberPicker
                value={hour}
                min={0}
                max={23}
                onChange={setHour}
                label={t('settings.hour')}
                colors={COLORS}
                isDark={isDark}
              />
              <Text style={{ color: COLORS.text, fontSize: 32, fontWeight: '600', alignSelf: 'center' }}>:</Text>
              <NumberPicker
                value={minute}
                min={0}
                max={59}
                onChange={setMinute}
                label={t('settings.minute')}
                colors={COLORS}
                isDark={isDark}
              />
            </View>
            <TouchableOpacity
              style={[styles.modalButton, { backgroundColor: COLORS.accent }]}
              onPress={() => setShowTimeModal(false)}
            >
              <Text style={styles.modalButtonText}>{t('common.confirm')}</Text>
            </TouchableOpacity>
          </View>
        </TouchableOpacity>
      </Modal>
    </View>
  );
}
