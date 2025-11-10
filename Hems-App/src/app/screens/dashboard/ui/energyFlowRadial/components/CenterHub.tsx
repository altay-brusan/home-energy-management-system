import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import Svg, { Path } from 'react-native-svg';
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
 * Contains the N2N logo in the center.
 */
export const CenterHub: React.FC<CenterHubProps> = ({ size = 60, style }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  // Logo size should be ~55% of circle size for better fit
  const logoWidth = size * 0.55;
  const logoHeight = (logoWidth * 141.97) / 452.5; // Maintain aspect ratio

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
      {/* N2N Logo - Centered */}
      <Svg
        width={logoWidth}
        height={logoHeight}
        viewBox="0 0 452.5 141.97"
      >
        <Path
          d="M98.97,141.97L24.9,38.52v103.45H0V0h25.54l72.58,100.68V0h24.9v141.97h-24.05Z"
          fill={COLORS.textSoft}
        />
        <Path
          d="M187.14,38.87c29.42,0,48.29,22.52,48.29,53.36v5.08h-75.69c1.62,14.81,12.78,26.99,31.25,26.99c9.54,0,20.9-3.86,27.8-10.76l9.74,14c-9.74,9.33-24.15,14.2-39.77,14.2-29.42,0-51.34-20.49-51.34-51.54,0-28.41,20.7-51.33,49.71-51.33ZM159.54,82.29h55.19c-.41-11.57-8.32-25.97-27.59-25.97-18.26,0-26.58,14-27.6,25.97Z"
          fill={COLORS.textSoft}
        />
        <Path
          d="M254.35,108.05v-49.31h-6.94l-11.97-19.58h18.91V11.07h22.35v28.1h20.86v19.58h-20.86v47.8c0,8.87,7.19,16.05,16.05,16.05h4.81s0,19.37,0,19.37h-9.29c-18.73,0-33.92-15.18-33.92-33.92Z"
          fill={COLORS.textSoft}
        />
        <Path
          d="M313.5,122.6l48.91-63.85h-48.91v-19.58h76.27s0,19.58,0,19.58l-48.85,63.85h48.85v19.37h-76.27v-19.37Z"
          fill={COLORS.textSoft}
        />
        <Path
          d="M403.64,141.97v-70.71c0-17.72,14.37-32.09,32.09-32.09h16.78v19.58h-13.36c-7.27,0-13.16,5.89-13.16,13.16v70.07h-22.35Z"
          fill={COLORS.textSoft}
        />
      </Svg>
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
