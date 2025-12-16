/**
 * ModeCard Component
 * Grid görünümündeki mod kartı (3 sütun layout için)
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Dimensions } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import {
  PiggyBank,
  TrendingUp,
  ShieldCheck,
  Moon,
  CircleOff,
  Fuel,
  Scale,
  SlidersHorizontal,
  LucideIcon,
} from 'lucide-react-native';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

// Mode ID'ye göre ikon eşlemesi
const MODE_ICONS: Record<string, LucideIcon> = {
  mode_1: PiggyBank,        // Maksimum Tasarruf
  mode_2: TrendingUp,       // Şebekeye Satış
  mode_4: ShieldCheck,      // Yedek Güç (UPS)
  mode_5: Moon,             // Gece Tarifesi Avantajı
  mode_6: CircleOff,        // Sıfır İhracat
  mode_7: Fuel,             // Jeneratör Destekli
  mode_8: Scale,            // Dengeli Hibrit
  custom: SlidersHorizontal, // Özel Mod
};

const { width: SCREEN_WIDTH } = Dimensions.get('window');
const CARD_MARGIN = 8;
const CARD_WIDTH = (SCREEN_WIDTH - 48 - CARD_MARGIN * 4) / 3; // 3 columns

interface ModeCardProps {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  isCustom: boolean;
  onPress: () => void;
}

const ModeCard: React.FC<ModeCardProps> = ({
  id,
  name,
  description,
  isActive,
  isCustom,
  onPress,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const IconComponent = MODE_ICONS[id];

  // Preset mode mavi rengi (TodaySummary ile aynı)
  const PRESET_BLUE = '#5EB2F7';

  // İkon rengi
  const iconColor = isActive
    ? COLORS.accent
    : isCustom
    ? '#9C27B0'
    : PRESET_BLUE;

  // İkon arka plan rengi
  const iconBgColor = isActive
    ? `${COLORS.accent}20`
    : isCustom
    ? '#9C27B015'
    : `${PRESET_BLUE}15`;

  return (
    <TouchableOpacity
      onPress={onPress}
      activeOpacity={0.7}
      style={[
        styles.card,
        {
          backgroundColor: isActive
            ? `${COLORS.accent}15`
            : isDark
            ? COLORS.surface
            : COLORS.white,
          borderColor: isActive
            ? COLORS.accent
            : isCustom
            ? '#9C27B0'
            : COLORS.border,
          borderWidth: isActive ? 2 : isCustom ? 1.5 : 1,
          borderStyle: isCustom && !isActive ? 'dashed' : 'solid',
        },
      ]}
    >
      {/* Mode Icon */}
      <View
        style={[
          styles.iconContainer,
          {
            backgroundColor: iconBgColor,
          },
        ]}
      >
        {IconComponent && (
          <IconComponent size={24} color={iconColor} strokeWidth={2} />
        )}
      </View>

      {/* Mode Name */}
      <Text
        style={[
          styles.name,
          {
            color: isActive ? COLORS.accent : COLORS.text,
          },
        ]}
        numberOfLines={2}
      >
        {name}
      </Text>

      {/* Active Indicator */}
      {isActive && (
        <View style={styles.activeIndicator}>
          <MaterialIcons name="check-circle" size={18} color={COLORS.accent} />
        </View>
      )}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    width: CARD_WIDTH,
    minHeight: 110,
    borderRadius: 12,
    padding: 12,
    margin: CARD_MARGIN,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
  },
  iconContainer: {
    width: 48,
    height: 48,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 8,
  },
  name: {
    fontSize: 11,
    fontWeight: '600',
    textAlign: 'center',
    lineHeight: 14,
  },
  activeIndicator: {
    position: 'absolute',
    top: 8,
    right: 8,
  },
});

export default ModeCard;
