import React from 'react';
import { View, StyleProp, ViewStyle } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getCardVariantStyle } from './Card.styles';

type Variant = 'elevated' | 'outlined' | 'ghost' | 'surfaceDark';

type CardProps = {
  style?: StyleProp<ViewStyle>;
  padding?: number;
  radius?: number;
  variant?: Variant;
  children: React.ReactNode;
};

export default function Card({
  children,
  style,
  padding = 16,
  radius = 18,
  variant = 'elevated',
}: CardProps) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const variantStyle = getCardVariantStyle(variant, radius, isDark);

  return <View style={[variantStyle, { padding }, style]}>{children}</View>;
}
