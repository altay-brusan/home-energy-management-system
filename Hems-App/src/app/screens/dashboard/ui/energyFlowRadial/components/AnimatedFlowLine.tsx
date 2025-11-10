import React, { useEffect } from 'react';
import { G, Line, Circle } from 'react-native-svg';
import Animated, {
  useSharedValue,
  withRepeat,
  withTiming,
  Easing,
  useAnimatedProps,
  cancelAnimation,
} from 'react-native-reanimated';

const AnimatedCircle = Animated.createAnimatedComponent(Circle);

interface AnimatedFlowLineProps {
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  direction: 'forward' | 'backward';
  active: boolean;
  color?: string;
  baseColor?: string;
}

const AnimatedFlowLineComponent: React.FC<AnimatedFlowLineProps> = ({
  startX,
  startY,
  endX,
  endY,
  direction,
  active,
  color = '#3B82F6',
  baseColor = '#CBD5E1',
}) => {
  // Moving dot animation
  const dotProgress = useSharedValue(0);

  useEffect(() => {
    // Cancel any existing animation first
    cancelAnimation(dotProgress);

    if (active) {
      // Reset to 0 first
      dotProgress.value = 0;

      // Then start moving dot animation
      dotProgress.value = withRepeat(
        withTiming(1, {
          duration: 2000,
          easing: Easing.linear,
        }),
        -1, // Infinite repeat
        false
      );
    } else {
      // When inactive, set to 0
      dotProgress.value = 0;
    }

    return () => {
      // Cleanup: cancel animation when component unmounts or dependencies change
      cancelAnimation(dotProgress);
    };
  }, [active, direction]);

  // Calculate moving dot positions along the line (3 dots with offset)
  const animatedDot1Props = useAnimatedProps(() => {
    'worklet';
    // Forward: startX -> endX (normal direction)
    // Backward: endX -> startX (reverse direction)
    let progress = direction === 'forward' ? dotProgress.value : 1 - dotProgress.value;
    return {
      cx: startX + (endX - startX) * progress,
      cy: startY + (endY - startY) * progress,
      opacity: active ? 0.8 : 0,
    };
  });

  const animatedDot2Props = useAnimatedProps(() => {
    'worklet';
    let baseProgress = (dotProgress.value + 0.33) % 1;
    let progress = direction === 'forward' ? baseProgress : 1 - baseProgress;
    return {
      cx: startX + (endX - startX) * progress,
      cy: startY + (endY - startY) * progress,
      opacity: active ? 0.8 : 0,
    };
  });

  const animatedDot3Props = useAnimatedProps(() => {
    'worklet';
    let baseProgress = (dotProgress.value + 0.66) % 1;
    let progress = direction === 'forward' ? baseProgress : 1 - baseProgress;
    return {
      cx: startX + (endX - startX) * progress,
      cy: startY + (endY - startY) * progress,
      opacity: active ? 0.8 : 0,
    };
  });

  return (
    <G>
      {/* Static base line (gray) */}
      <Line
        x1={startX}
        y1={startY}
        x2={endX}
        y2={endY}
        stroke={baseColor}
        strokeWidth={3}
        strokeLinecap="round"
      />

      {/* Moving dots animation (only visible when active) */}
      {active && (
        <>
          <AnimatedCircle
            r={2.5}
            fill={color}
            animatedProps={animatedDot1Props}
          />
          <AnimatedCircle
            r={2.5}
            fill={color}
            animatedProps={animatedDot2Props}
          />
          <AnimatedCircle
            r={2.5}
            fill={color}
            animatedProps={animatedDot3Props}
          />
        </>
      )}
    </G>
  );
};

export const AnimatedFlowLine = React.memo(AnimatedFlowLineComponent);
export default AnimatedFlowLine;
