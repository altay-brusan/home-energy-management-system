import React from 'react';
import Svg, { Rect } from 'react-native-svg';

interface BatteryIconProps {
  percentage: number;
  size?: number;
  fillColor?: string;
  borderColor?: string;
}

export const BatteryIcon: React.FC<BatteryIconProps> = ({
  percentage,
  size = 24,
  fillColor = '#22C55E',
  borderColor = '#9CA3AF',
}) => {
  const clampedPercentage = Math.max(0, Math.min(100, percentage));
  const fillWidth = (clampedPercentage / 100) * 16; // Max fill width is 16

  // Color based on percentage
  const getFillColor = () => {
    if (clampedPercentage <= 20) return '#EF4444'; // Red
    if (clampedPercentage <= 40) return '#F59E0B'; // Orange
    return fillColor; // Green
  };

  return (
    <Svg width={size} height={size} viewBox="0 0 24 24">
      {/* Battery body outline */}
      <Rect
        x="2"
        y="7"
        width="18"
        height="10"
        rx="2"
        ry="2"
        fill="none"
        stroke={borderColor}
        strokeWidth="1.5"
      />
      {/* Battery terminal */}
      <Rect
        x="20"
        y="10"
        width="2"
        height="4"
        rx="0.5"
        fill={borderColor}
      />
      {/* Battery fill level */}
      {clampedPercentage > 0 && (
        <Rect
          x="4"
          y="9"
          width={fillWidth}
          height="6"
          rx="1"
          fill={getFillColor()}
        />
      )}
    </Svg>
  );
};

export default BatteryIcon;
