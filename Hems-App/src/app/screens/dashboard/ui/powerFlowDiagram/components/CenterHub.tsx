import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { useThemeStore } from '../../../../../../store/themeStore';
import { getThemeColors } from '../../../../../../theme/constants';

interface CenterHubProps {
  size?: number;
  style?: ViewStyle;
}

// GRP Logo Icon
const GrpLogo: React.FC<{ size: number }> = ({ size }) => (
  <Svg width={size} height={size} viewBox="0 0 62.56 62.56">
    <Path
      d="M31.28,0C14.01,0,0,14.01,0,31.28s14.01,31.28,31.28,31.28,31.28-14.01,31.28-31.28S48.56,0,31.28,0M31.4,52.23c-11.55,0-20.95-9.4-20.95-20.95S19.85,10.34,31.4,10.34h.28v3.91h-.28c-9.4,0-17.04,7.64-17.04,17.04s7.64,17.04,17.04,17.04c8.35,0,15.31-6.03,16.76-13.97h3.97c-1.49,10.1-10.21,17.88-20.72,17.88M41.46,33.19h-9.79v-3.93h9.79v3.93Z"
      fill="#00a270"
    />
  </Svg>
);

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

  // Logo size should be smaller than the hub to fit inside with some padding
  const logoSize = size * 0.55;

  return (
    <View
      style={[
        styles.container,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
          borderColor: COLORS.border,
        },
        style,
      ]}
    >
      <GrpLogo size={logoSize} />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    borderWidth: 4,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 2,
  },
});

export default CenterHub;
