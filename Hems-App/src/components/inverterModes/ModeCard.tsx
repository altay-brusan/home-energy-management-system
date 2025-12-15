/**
 * ModeCard Component
 * Grid görünümündeki mod kartı (3 sütun layout için)
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Dimensions } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { getModeNumber } from '../../data/presetModes';

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

  const modeNumber = getModeNumber(id);

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
      {/* Mode Number Badge */}
      <View
        style={[
          styles.badge,
          {
            backgroundColor: isActive
              ? COLORS.accent
              : isCustom
              ? '#9C27B0'
              : isDark
              ? COLORS.border
              : COLORS.gray200,
          },
        ]}
      >
        <Text
          style={[
            styles.badgeText,
            {
              color: isActive || isCustom ? '#FFFFFF' : COLORS.textSoft,
            },
          ]}
        >
          {modeNumber}
        </Text>
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
    minHeight: 100,
    borderRadius: 12,
    padding: 12,
    margin: CARD_MARGIN,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
  },
  badge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    marginBottom: 8,
  },
  badgeText: {
    fontSize: 12,
    fontWeight: '700',
  },
  name: {
    fontSize: 12,
    fontWeight: '600',
    textAlign: 'center',
    lineHeight: 16,
  },
  activeIndicator: {
    position: 'absolute',
    top: 8,
    right: 8,
  },
});

export default ModeCard;
