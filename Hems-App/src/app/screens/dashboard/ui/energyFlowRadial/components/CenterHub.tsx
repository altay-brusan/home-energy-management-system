import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { useThemeStore } from '../../../../../../store/themeStore';
import { getThemeColors } from '../../../../../../theme/constants';

interface CenterHubProps {
  size?: number;
  style?: ViewStyle;
}

/**
 * CenterHub Component
 *
 * Represents the central inverter/system hub in the radial energy flow diagram.
 * All energy flows pass through this central point.
 */
export const CenterHub: React.FC<CenterHubProps> = ({ size = 60, style }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <View
      style={[
        styles.container,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          backgroundColor: isDark ? COLORS.surface : '#E5E7EB',
          borderColor: isDark ? COLORS.border : '#9CA3AF',
        },
        style,
      ]}
    />
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    borderWidth: 3,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 2,
  },
});

export default CenterHub;
