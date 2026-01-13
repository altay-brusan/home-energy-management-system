import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import Svg, { Path, G } from 'react-native-svg';
import { useThemeStore } from '../../../../../../store/themeStore';
import { getThemeColors } from '../../../../../../theme/constants';

interface CenterHubProps {
  size?: number;
  style?: ViewStyle;
  isActive?: boolean;
}

// Netzr Logo Icon
const NetzrLogo: React.FC<{ size: number }> = ({ size }) => (
  <Svg width={size} height={size * 0.31} viewBox="0 0 420.25 131.85">
    <G>
      <Path
        d="M91.92,131.85L23.13,35.78v96.07H0V0h23.72l67.41,93.5V0h23.13v131.85h-22.34Z"
        fill="#2f52a0"
      />
      <Path
        d="M173.8,36.1c27.32,0,44.85,20.92,44.85,49.56v4.71h-70.29c1.51,13.76,11.87,25.06,29.02,25.06,8.86,0,19.41-3.58,25.82-9.99l9.05,13c-9.05,8.67-22.43,13.19-36.94,13.19-27.32,0-47.68-19.03-47.68-47.87,0-26.38,19.22-47.68,46.17-47.68ZM148.17,76.43h51.26c-.38-10.74-7.73-24.12-25.63-24.12-16.96,0-24.69,13-25.63,24.12Z"
        fill="#2f52a0"
      />
      <Path
        d="M236.22,100.35v-45.79h-6.45l-11.12-18.19h17.57V10.28h20.76v26.09h19.37v18.19h-19.37v44.39c0,8.23,6.67,14.91,14.91,14.91h4.46s0,17.99,0,17.99h-8.63c-17.4,0-31.5-14.1-31.5-31.5Z"
        fill="#2f52a0"
      />
      <Path
        d="M291.16,113.86l45.43-59.3h-45.43v-18.19h70.83s0,18.19,0,18.19l-45.37,59.3h45.37v17.99h-70.83v-17.99Z"
        fill="#2f52a0"
      />
      <Path
        d="M374.87,131.85v-65.67c0-16.46,13.34-29.8,29.8-29.8h15.58v18.19h-12.41c-6.75,0-12.22,5.47-12.22,12.22v65.07h-20.76Z"
        fill="#2f52a0"
      />
    </G>
  </Svg>
);

/**
 * CenterHub Component
 *
 * Represents the central inverter/system hub in the radial energy flow diagram.
 * All energy flows pass through this central point.
 */
export const CenterHub: React.FC<CenterHubProps> = ({ size = 60, style, isActive = false }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  // Logo size should be smaller than the hub to fit inside with some padding
  const logoSize = size * 0.45;

  return (
    <View
      style={[
        styles.container,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
          borderColor: isActive ? '#2F52A0' : COLORS.border,
        },
        style,
      ]}
    >
      <NetzrLogo size={logoSize} />
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
