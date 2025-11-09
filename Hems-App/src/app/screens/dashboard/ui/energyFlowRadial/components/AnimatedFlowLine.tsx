import React, { useEffect } from 'react';
import { G, Line, Defs, LinearGradient, Stop } from 'react-native-svg';
import Animated, {
  useSharedValue,
  withRepeat,
  withTiming,
  Easing,
  useAnimatedProps,
} from 'react-native-reanimated';

const AnimatedLine = Animated.createAnimatedComponent(Line);

interface AnimatedFlowLineProps {
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  direction: 'forward' | 'backward';
  active: boolean;
  color?: string;
}

export const AnimatedFlowLine: React.FC<AnimatedFlowLineProps> = ({
  startX,
  startY,
  endX,
  endY,
  direction,
  active,
  color = '#3B82F6',
}) => {
  // Animated value for dash offset
  const dashOffset = useSharedValue(0);

  useEffect(() => {
    if (active) {
      // Animate dash offset for flowing effect
      // Forward: 0 -> 20, Backward: 20 -> 0
      const startValue = direction === 'forward' ? 0 : 20;
      const endValue = direction === 'forward' ? 20 : 0;

      dashOffset.value = withRepeat(
        withTiming(endValue, {
          duration: 1500,
          easing: Easing.linear,
        }),
        -1, // Infinite repeat
        false
      );
    } else {
      // Reset when inactive
      dashOffset.value = 0;
    }
  }, [active, direction, dashOffset]);

  // Animated props for the flowing line
  const animatedProps = useAnimatedProps(() => {
    return {
      strokeDashoffset: dashOffset.value,
    };
  });

  // Calculate gradient ID based on position (unique for each line)
  const gradientId = `flowGradient-${startX}-${startY}-${endX}-${endY}`;

  return (
    <G>
      {/* Static base line (gray) */}
      <Line
        x1={startX}
        y1={startY}
        x2={endX}
        y2={endY}
        stroke="#CBD5E1"
        strokeWidth={3}
        strokeLinecap="round"
      />

      {/* Animated flow line (only visible when active) */}
      {active && (
        <>
          <Defs>
            <LinearGradient id={gradientId} x1="0%" y1="0%" x2="100%" y2="0%">
              <Stop offset="0%" stopColor={color} stopOpacity={0.3} />
              <Stop offset="50%" stopColor={color} stopOpacity={1} />
              <Stop offset="100%" stopColor={color} stopOpacity={0.3} />
            </LinearGradient>
          </Defs>
          <AnimatedLine
            x1={startX}
            y1={startY}
            x2={endX}
            y2={endY}
            stroke={`url(#${gradientId})`}
            strokeWidth={3}
            strokeDasharray="10, 10"
            strokeLinecap="round"
            animatedProps={animatedProps}
          />
        </>
      )}
    </G>
  );
};

export default AnimatedFlowLine;
