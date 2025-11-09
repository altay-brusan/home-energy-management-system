import React from 'react';
import { View, Text, StyleSheet, ViewStyle } from 'react-native';
import Svg, { Path, Rect, Circle, G } from 'react-native-svg';
import { useThemeStore } from '../../../../../../store/themeStore';
import { getThemeColors } from '../../../../../../theme/constants';

type NodeType = 'pv' | 'battery' | 'grid' | 'load';

interface EnergyNodeProps {
  type: NodeType;
  power: number;
  active: boolean;
  style?: ViewStyle;
  unit?: string;
}

// SVG Icons
const PVIcon: React.FC<{ active: boolean; color: string }> = ({ active, color }) => (
  <Svg width={48} height={48} viewBox="0 0 24 24" fill="none">
    {/* Sun rays */}
    <Path
      d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"
      stroke={active ? color : '#9CA3AF'}
      strokeWidth={2}
      strokeLinecap="round"
    />
    {/* Sun center */}
    <Circle cx={12} cy={12} r={4} stroke={active ? color : '#9CA3AF'} strokeWidth={2} fill="none" />
  </Svg>
);

const BatteryIcon: React.FC<{ active: boolean; color: string }> = ({ active, color }) => (
  <Svg width={48} height={48} viewBox="0 0 24 24" fill="none">
    {/* Battery body */}
    <Rect
      x={3}
      y={7}
      width={15}
      height={10}
      rx={2}
      stroke={active ? color : '#9CA3AF'}
      strokeWidth={2}
      fill="none"
    />
    {/* Battery charge level */}
    <Rect x={5} y={9} width={6} height={6} fill={active ? color : '#9CA3AF'} />
    {/* Battery terminal */}
    <Path d="M18 11h3v2h-3" stroke={active ? color : '#9CA3AF'} strokeWidth={2} />
  </Svg>
);

const GridIcon: React.FC<{ active: boolean; color: string }> = ({ active, color }) => (
  <Svg width={48} height={48} viewBox="0 0 24 24" fill="none">
    {/* Lightning bolt */}
    <Path
      d="M13 2L3 14h8l-1 8 10-12h-8l1-8z"
      stroke={active ? color : '#9CA3AF'}
      strokeWidth={2}
      strokeLinejoin="round"
      fill="none"
    />
  </Svg>
);

const LoadIcon: React.FC<{ active: boolean; color: string }> = ({ active, color }) => (
  <Svg width={48} height={48} viewBox="0 0 24 24" fill="none">
    {/* House outline */}
    <Path
      d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"
      stroke={active ? color : '#9CA3AF'}
      strokeWidth={2}
      fill="none"
    />
    {/* Door */}
    <Path d="M9 22V12h6v10" stroke={active ? color : '#9CA3AF'} strokeWidth={2} />
  </Svg>
);

const iconMap = {
  pv: PVIcon,
  battery: BatteryIcon,
  grid: GridIcon,
  load: LoadIcon,
};

// Color mapping for each node type
const colorMap: Record<NodeType, string> = {
  pv: '#FDB52E',       // Orange
  battery: '#12C6A9',  // Teal
  grid: '#9D76F2',     // Purple
  load: '#FF6B6B',     // Red
};

/**
 * Format power value for display
 * - Values >= 1000W shown as kW (e.g., "2.5 kW")
 * - Values < 1000W shown as W (e.g., "450 W")
 */
const formatPower = (watts: number): string => {
  if (watts >= 1000) {
    return `${(watts / 1000).toFixed(1)}`;
  }
  return `${Math.round(watts)}`;
};

const getPowerUnit = (watts: number): string => {
  return watts >= 1000 ? 'kW' : 'W';
};

export const EnergyNode: React.FC<EnergyNodeProps> = ({
  type,
  power,
  active,
  style,
  unit,
}) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const Icon = iconMap[type];
  const nodeColor = colorMap[type];

  const displayUnit = unit || getPowerUnit(power);
  const formattedPower = formatPower(power);

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
          borderColor: active ? nodeColor : COLORS.border,
          borderWidth: 2,
          opacity: active ? 1 : 0.4,
        },
        style,
      ]}
    >
      <Icon active={active} color={nodeColor} />
      <Text
        style={[
          styles.powerValue,
          {
            color: active ? COLORS.textSoft : COLORS.slate400,
            fontWeight: '700',
          },
        ]}
      >
        {formattedPower}
      </Text>
      <Text
        style={[
          styles.powerUnit,
          {
            color: active ? COLORS.textSoft : COLORS.border,
          },
        ]}
      >
        {displayUnit}
      </Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    width: 100,
    height: 100,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  powerValue: {
    fontSize: 18,
    marginTop: 4,
  },
  powerUnit: {
    fontSize: 12,
    marginTop: 2,
  },
});

export default EnergyNode;
