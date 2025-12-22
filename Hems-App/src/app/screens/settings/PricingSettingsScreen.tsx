import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Modal,
  Alert,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Plus, Trash2, Clock } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import {
  useSettingsStore,
  PricingType,
  TimePeriodPrice,
  Currency,
  CURRENCY_SYMBOLS,
} from '../../../store/settingsStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makePricingSettingsScreenStyles } from './PricingSettingsScreen.styles';

interface SegmentedButtonProps {
  options: { label: string; value: string }[];
  selectedValue: string;
  onSelect: (value: string) => void;
  colors: Record<string, string>;
  isDark: boolean;
}

const SegmentedButton: React.FC<SegmentedButtonProps> = ({
  options,
  selectedValue,
  onSelect,
  colors,
  isDark,
}) => {
  return (
    <View
      style={{
        flexDirection: 'row',
        backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
        borderRadius: 8,
        padding: 3,
      }}
    >
      {options.map((option) => {
        const isSelected = option.value === selectedValue;
        return (
          <TouchableOpacity
            key={option.value}
            onPress={() => onSelect(option.value)}
            style={{
              flex: 1,
              paddingVertical: 10,
              paddingHorizontal: 16,
              borderRadius: 6,
              backgroundColor: isSelected ? colors.accent : 'transparent',
              alignItems: 'center',
            }}
          >
            <Text
              style={{
                fontSize: 14,
                fontWeight: isSelected ? '600' : '500',
                color: isSelected ? '#fff' : colors.textSoft,
              }}
            >
              {option.label}
            </Text>
          </TouchableOpacity>
        );
      })}
    </View>
  );
};

interface PriceInputProps {
  label: string;
  value: number;
  onChangeValue: (value: number) => void;
  colors: Record<string, string>;
  isDark: boolean;
  currencySymbol: string;
}

const PriceInput: React.FC<PriceInputProps> = ({
  label,
  value,
  onChangeValue,
  colors,
  isDark,
  currencySymbol,
}) => {
  const [inputValue, setInputValue] = useState(value.toString());

  const handleChangeText = (text: string) => {
    // Allow only numbers and decimal point
    const filtered = text.replace(/[^0-9.]/g, '');
    setInputValue(filtered);
    const num = parseFloat(filtered);
    if (!isNaN(num)) {
      onChangeValue(num);
    }
  };

  return (
    <View style={{ marginBottom: 16 }}>
      <Text
        style={{
          fontSize: 14,
          fontWeight: '500',
          color: colors.textSoft,
          marginBottom: 8,
        }}
      >
        {label}
      </Text>
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'center',
          backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)',
          borderRadius: 10,
          paddingHorizontal: 14,
          borderWidth: 1,
          borderColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)',
        }}
      >
        <Text style={{ fontSize: 18, color: colors.accent, fontWeight: '600' }}>{currencySymbol}</Text>
        <TextInput
          style={{
            flex: 1,
            paddingVertical: 14,
            paddingHorizontal: 10,
            fontSize: 16,
            color: colors.text,
          }}
          value={inputValue}
          onChangeText={handleChangeText}
          keyboardType="decimal-pad"
          placeholder="0.00"
          placeholderTextColor={colors.textSoft}
        />
        <Text style={{ fontSize: 14, color: colors.textSoft }}>/kWh</Text>
      </View>
    </View>
  );
};

export default function PricingSettingsScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makePricingSettingsScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  const {
    pricingType,
    currency,
    fixedBuyPrice,
    fixedSellPrice,
    dynamicPrices,
    setPricingType,
    setCurrency,
    setFixedBuyPrice,
    setFixedSellPrice,
    updateDynamicPeriod,
    addDynamicPeriod,
    removeDynamicPeriod,
  } = useSettingsStore();

  const currencySymbol = CURRENCY_SYMBOLS[currency];

  const currencyOptions: { label: string; value: Currency }[] = [
    { label: `€ EUR`, value: 'EUR' },
    { label: `$ USD`, value: 'USD' },
    { label: `£ GBP`, value: 'GBP' },
    { label: `₺ TRY`, value: 'TRY' },
  ];

  const [editingPeriod, setEditingPeriod] = useState<TimePeriodPrice | null>(null);
  const [showTimeModal, setShowTimeModal] = useState(false);
  const [editingField, setEditingField] = useState<'startTime' | 'endTime' | null>(null);
  const [tempTime, setTempTime] = useState({ hours: '00', minutes: '00' });

  const handleAddPeriod = () => {
    const newId = Date.now().toString();
    addDynamicPeriod({
      id: newId,
      startTime: '00:00',
      endTime: '06:00',
      buyPrice: 0.20,
      sellPrice: 0.10,
    });
  };

  const handleDeletePeriod = (id: string) => {
    Alert.alert(
      t('pricing.deletePeriod'),
      t('pricing.deletePeriodConfirm'),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('common.delete'),
          style: 'destructive',
          onPress: () => removeDynamicPeriod(id),
        },
      ]
    );
  };

  const openTimeModal = (period: TimePeriodPrice, field: 'startTime' | 'endTime') => {
    const time = field === 'startTime' ? period.startTime : period.endTime;
    const [hours, minutes] = time.split(':');
    setTempTime({ hours, minutes });
    setEditingPeriod(period);
    setEditingField(field);
    setShowTimeModal(true);
  };

  const saveTime = () => {
    if (editingPeriod && editingField) {
      const newTime = `${tempTime.hours.padStart(2, '0')}:${tempTime.minutes.padStart(2, '0')}`;
      updateDynamicPeriod(editingPeriod.id, { [editingField]: newTime });
    }
    setShowTimeModal(false);
    setEditingPeriod(null);
    setEditingField(null);
  };

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>{t('pricing.title')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Currency Selection */}
        <View style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <Text style={[styles.cardTitle, { color: COLORS.text }]}>
            {t('pricing.currency')}
          </Text>
          <Text style={[styles.cardDesc, { color: COLORS.textSoft }]}>
            {t('pricing.currencyDesc')}
          </Text>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 4 }}>
            {currencyOptions.map((option) => {
              const isSelected = option.value === currency;
              return (
                <TouchableOpacity
                  key={option.value}
                  onPress={() => setCurrency(option.value)}
                  style={{
                    paddingVertical: 10,
                    paddingHorizontal: 16,
                    borderRadius: 8,
                    backgroundColor: isSelected ? COLORS.accent : isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
                    borderWidth: 1,
                    borderColor: isSelected ? COLORS.accent : isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)',
                  }}
                >
                  <Text
                    style={{
                      fontSize: 14,
                      fontWeight: isSelected ? '600' : '500',
                      color: isSelected ? '#fff' : COLORS.textSoft,
                    }}
                  >
                    {option.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Pricing Type Selection */}
        <View style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <Text style={[styles.cardTitle, { color: COLORS.text }]}>
            {t('pricing.pricingType')}
          </Text>
          <Text style={[styles.cardDesc, { color: COLORS.textSoft }]}>
            {t('pricing.pricingTypeDesc')}
          </Text>
          <SegmentedButton
            options={[
              { label: t('pricing.fixed'), value: 'fixed' },
              { label: t('pricing.dynamic'), value: 'dynamic' },
            ]}
            selectedValue={pricingType}
            onSelect={(value) => setPricingType(value as PricingType)}
            colors={COLORS}
            isDark={isDark}
          />
        </View>

        {/* Fixed Pricing */}
        {pricingType === 'fixed' && (
          <View style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('pricing.fixedPricing')}
            </Text>
            <PriceInput
              label={t('pricing.buyPriceLabel', { symbol: currencySymbol })}
              value={fixedBuyPrice}
              onChangeValue={setFixedBuyPrice}
              colors={COLORS}
              isDark={isDark}
              currencySymbol={currencySymbol}
            />
            <PriceInput
              label={t('pricing.sellPriceLabel', { symbol: currencySymbol })}
              value={fixedSellPrice}
              onChangeValue={setFixedSellPrice}
              colors={COLORS}
              isDark={isDark}
              currencySymbol={currencySymbol}
            />
          </View>
        )}

        {/* Dynamic Pricing */}
        {pricingType === 'dynamic' && (
          <>
            <View style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
              <View style={styles.cardHeader}>
                <Text style={[styles.cardTitle, { color: COLORS.text }]}>
                  {t('pricing.dynamicPricing')}
                </Text>
                <TouchableOpacity onPress={handleAddPeriod} style={styles.addButton}>
                  <Plus size={20} color={COLORS.accent} />
                  <Text style={[styles.addButtonText, { color: COLORS.accent }]}>
                    {t('pricing.addPeriod')}
                  </Text>
                </TouchableOpacity>
              </View>
              <Text style={[styles.cardDesc, { color: COLORS.textSoft }]}>
                {t('pricing.dynamicPricingDesc')}
              </Text>
            </View>

            {dynamicPrices.map((period, index) => (
              <View
                key={period.id}
                style={[styles.periodCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
              >
                <View style={styles.periodHeader}>
                  <View style={styles.periodBadge}>
                    <Clock size={14} color={COLORS.accent} />
                    <Text style={[styles.periodBadgeText, { color: COLORS.accent }]}>
                      {t('pricing.period')} {index + 1}
                    </Text>
                  </View>
                  <TouchableOpacity
                    onPress={() => handleDeletePeriod(period.id)}
                    style={styles.deleteButton}
                  >
                    <Trash2 size={18} color="#EF4444" />
                  </TouchableOpacity>
                </View>

                {/* Time Range */}
                <View style={styles.timeRow}>
                  <TouchableOpacity
                    style={[styles.timeButton, { backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)' }]}
                    onPress={() => openTimeModal(period, 'startTime')}
                  >
                    <Text style={[styles.timeLabel, { color: COLORS.textSoft }]}>
                      {t('pricing.startTime')}
                    </Text>
                    <Text style={[styles.timeValue, { color: COLORS.text }]}>
                      {period.startTime}
                    </Text>
                  </TouchableOpacity>
                  <Text style={[styles.timeSeparator, { color: COLORS.textSoft }]}>—</Text>
                  <TouchableOpacity
                    style={[styles.timeButton, { backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)' }]}
                    onPress={() => openTimeModal(period, 'endTime')}
                  >
                    <Text style={[styles.timeLabel, { color: COLORS.textSoft }]}>
                      {t('pricing.endTime')}
                    </Text>
                    <Text style={[styles.timeValue, { color: COLORS.text }]}>
                      {period.endTime}
                    </Text>
                  </TouchableOpacity>
                </View>

                {/* Prices */}
                <View style={styles.pricesRow}>
                  <View style={styles.priceColumn}>
                    <Text style={[styles.priceLabel, { color: COLORS.textSoft }]}>
                      {t('pricing.buyPrice')}
                    </Text>
                    <View
                      style={[
                        styles.priceInputContainer,
                        { backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)' },
                      ]}
                    >
                      <Text style={[styles.currencySymbol, { color: COLORS.accent }]}>{currencySymbol}</Text>
                      <TextInput
                        style={[styles.priceInput, { color: COLORS.text }]}
                        value={period.buyPrice.toString()}
                        onChangeText={(text) => {
                          const num = parseFloat(text.replace(/[^0-9.]/g, ''));
                          if (!isNaN(num)) {
                            updateDynamicPeriod(period.id, { buyPrice: num });
                          }
                        }}
                        keyboardType="decimal-pad"
                        placeholder="0.00"
                        placeholderTextColor={COLORS.textSoft}
                      />
                    </View>
                  </View>
                  <View style={styles.priceColumn}>
                    <Text style={[styles.priceLabel, { color: COLORS.textSoft }]}>
                      {t('pricing.sellPrice')}
                    </Text>
                    <View
                      style={[
                        styles.priceInputContainer,
                        { backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)' },
                      ]}
                    >
                      <Text style={[styles.currencySymbol, { color: COLORS.accent }]}>{currencySymbol}</Text>
                      <TextInput
                        style={[styles.priceInput, { color: COLORS.text }]}
                        value={period.sellPrice.toString()}
                        onChangeText={(text) => {
                          const num = parseFloat(text.replace(/[^0-9.]/g, ''));
                          if (!isNaN(num)) {
                            updateDynamicPeriod(period.id, { sellPrice: num });
                          }
                        }}
                        keyboardType="decimal-pad"
                        placeholder="0.00"
                        placeholderTextColor={COLORS.textSoft}
                      />
                    </View>
                  </View>
                </View>
              </View>
            ))}
          </>
        )}
      </ScrollView>

      {/* Time Picker Modal */}
      <Modal visible={showTimeModal} transparent animationType="fade">
        <View style={styles.modalOverlay}>
          <View style={[styles.modalContent, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
            <Text style={[styles.modalTitle, { color: COLORS.text }]}>
              {editingField === 'startTime' ? t('pricing.startTime') : t('pricing.endTime')}
            </Text>
            <View style={styles.timePickerRow}>
              <View style={styles.timePickerColumn}>
                <Text style={[styles.timePickerLabel, { color: COLORS.textSoft }]}>
                  {t('pricing.hours')}
                </Text>
                <TextInput
                  style={[
                    styles.timePickerInput,
                    {
                      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)',
                      color: COLORS.text,
                    },
                  ]}
                  value={tempTime.hours}
                  onChangeText={(text) => {
                    const num = parseInt(text, 10);
                    if (!isNaN(num) && num >= 0 && num <= 23) {
                      setTempTime({ ...tempTime, hours: text });
                    } else if (text === '') {
                      setTempTime({ ...tempTime, hours: '' });
                    }
                  }}
                  keyboardType="number-pad"
                  maxLength={2}
                />
              </View>
              <Text style={[styles.timePickerColon, { color: COLORS.text }]}>:</Text>
              <View style={styles.timePickerColumn}>
                <Text style={[styles.timePickerLabel, { color: COLORS.textSoft }]}>
                  {t('pricing.minutes')}
                </Text>
                <TextInput
                  style={[
                    styles.timePickerInput,
                    {
                      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)',
                      color: COLORS.text,
                    },
                  ]}
                  value={tempTime.minutes}
                  onChangeText={(text) => {
                    const num = parseInt(text, 10);
                    if (!isNaN(num) && num >= 0 && num <= 59) {
                      setTempTime({ ...tempTime, minutes: text });
                    } else if (text === '') {
                      setTempTime({ ...tempTime, minutes: '' });
                    }
                  }}
                  keyboardType="number-pad"
                  maxLength={2}
                />
              </View>
            </View>
            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={[styles.modalButton, { backgroundColor: COLORS.border }]}
                onPress={() => setShowTimeModal(false)}
              >
                <Text style={[styles.modalButtonText, { color: COLORS.textSoft }]}>
                  {t('common.cancel')}
                </Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalButton, { backgroundColor: COLORS.accent }]}
                onPress={saveTime}
              >
                <Text style={[styles.modalButtonText, { color: '#fff' }]}>
                  {t('common.save')}
                </Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </View>
  );
}
