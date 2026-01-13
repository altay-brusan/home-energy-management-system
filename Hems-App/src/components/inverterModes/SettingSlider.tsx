/**
 * SettingSlider Component
 * Sürüklenebilir slider ile ayar komponenti
 */

import React, { useCallback } from 'react';
import { View, Text, StyleSheet, TextInput, LayoutChangeEvent } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import Animated, {
  useSharedValue,
  useAnimatedStyle,
  runOnJS,
  withSpring,
} from 'react-native-reanimated';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface SettingSliderProps {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  step?: number;
  unit?: string;
  description?: string;
  disabled?: boolean;
  showMinMax?: boolean;
}

const THUMB_SIZE = 28;

const SettingSlider: React.FC<SettingSliderProps> = ({
  label,
  value,
  onChange,
  min,
  max,
  step = 1,
  unit = '',
  description,
  disabled = false,
  showMinMax = true,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const sliderWidth = useSharedValue(0);
  const thumbPosition = useSharedValue(0);
  const startPosition = useSharedValue(0);
  const isGestureActive = useSharedValue(false);

  // Calculate position from value
  const valueToPosition = useCallback((val: number, width: number) => {
    'worklet';
    const percentage = (val - min) / (max - min);
    return percentage * (width - THUMB_SIZE);
  }, [min, max]);

  // Calculate value from position
  const positionToValue = useCallback((pos: number, width: number) => {
    'worklet';
    const percentage = pos / (width - THUMB_SIZE);
    const rawValue = percentage * (max - min) + min;
    // Round to step
    const steppedValue = Math.round(rawValue / step) * step;
    return Math.min(max, Math.max(min, steppedValue));
  }, [min, max, step]);

  const handleValueChange = useCallback((newValue: number) => {
    if (!disabled && newValue !== value) {
      onChange(newValue);
    }
  }, [disabled, value, onChange]);

  const handleTextChange = (text: string) => {
    if (disabled) return;
    const numValue = parseInt(text, 10);
    if (!isNaN(numValue)) {
      const clampedValue = Math.min(max, Math.max(min, numValue));
      // Round to step
      const steppedValue = Math.round(clampedValue / step) * step;
      onChange(steppedValue);
    }
  };

  const onLayout = (event: LayoutChangeEvent) => {
    const { width } = event.nativeEvent.layout;
    sliderWidth.value = width;
    thumbPosition.value = valueToPosition(value, width);
  };

  // Update thumb position when value changes externally
  React.useEffect(() => {
    if (sliderWidth.value > 0 && !isGestureActive.value) {
      thumbPosition.value = withSpring(valueToPosition(value, sliderWidth.value), {
        damping: 20,
        stiffness: 200,
      });
    }
  }, [value, sliderWidth.value, valueToPosition, isGestureActive.value, thumbPosition]);

  const panGesture = Gesture.Pan()
    .enabled(!disabled)
    .onBegin(() => {
      isGestureActive.value = true;
      startPosition.value = thumbPosition.value;
    })
    .onUpdate((event) => {
      const newPosition = Math.max(0, Math.min(sliderWidth.value - THUMB_SIZE, startPosition.value + event.translationX));
      thumbPosition.value = newPosition;
      const newValue = positionToValue(newPosition, sliderWidth.value);
      runOnJS(handleValueChange)(newValue);
    })
    .onEnd(() => {
      isGestureActive.value = false;
      // Snap to step
      const currentValue = positionToValue(thumbPosition.value, sliderWidth.value);
      thumbPosition.value = withSpring(valueToPosition(currentValue, sliderWidth.value), {
        damping: 20,
        stiffness: 200,
      });
    });

  const tapGesture = Gesture.Tap()
    .enabled(!disabled)
    .onEnd((event) => {
      const tapPosition = Math.max(0, Math.min(sliderWidth.value - THUMB_SIZE, event.x - THUMB_SIZE / 2));
      const newValue = positionToValue(tapPosition, sliderWidth.value);
      thumbPosition.value = withSpring(valueToPosition(newValue, sliderWidth.value), {
        damping: 20,
        stiffness: 200,
      });
      runOnJS(handleValueChange)(newValue);
    });

  const composedGesture = Gesture.Race(panGesture, tapGesture);

  const thumbAnimatedStyle = useAnimatedStyle(() => ({
    transform: [{ translateX: thumbPosition.value }],
  }));

  const progressAnimatedStyle = useAnimatedStyle(() => ({
    width: thumbPosition.value + THUMB_SIZE / 2,
  }));

  // Calculate progress percentage for display
  const progress = ((value - min) / (max - min)) * 100;

  return (
    <View
      style={[
        styles.container,
        { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
      ]}
    >
      <View style={styles.header}>
        <Text
          style={[
            styles.label,
            { color: disabled ? COLORS.textSoft : COLORS.text },
          ]}
        >
          {label}
        </Text>
        <View style={styles.valueContainer}>
          <TextInput
            style={[
              styles.valueInput,
              {
                color: COLORS.accent,
                backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
              },
            ]}
            value={String(value)}
            onChangeText={handleTextChange}
            keyboardType="numeric"
            editable={!disabled}
            selectTextOnFocus
          />
          <Text style={[styles.unit, { color: COLORS.accent }]}>{unit}</Text>
        </View>
      </View>

      {description && (
        <Text style={[styles.description, { color: COLORS.textSoft }]}>
          {description}
        </Text>
      )}

      {/* Slider Track */}
      <GestureDetector gesture={composedGesture}>
        <View
          style={[
            styles.sliderContainer,
            { backgroundColor: isDark ? '#333' : '#E0E0E0' },
          ]}
          onLayout={onLayout}
        >
          {/* Progress Fill */}
          <Animated.View
            style={[
              styles.progressFill,
              { backgroundColor: disabled ? COLORS.textSoft : COLORS.accent },
              progressAnimatedStyle,
            ]}
          />
          {/* Thumb */}
          <Animated.View
            style={[
              styles.thumb,
              {
                backgroundColor: disabled ? COLORS.textSoft : COLORS.accent,
                borderColor: isDark ? '#222' : '#fff',
              },
              thumbAnimatedStyle,
            ]}
          >
            <View style={styles.thumbInner} />
          </Animated.View>
        </View>
      </GestureDetector>

      {/* Min/Max Labels */}
      {showMinMax && (
        <View style={styles.rangeContainer}>
          <Text style={[styles.rangeText, { color: COLORS.textSoft }]}>
            {min}{unit}
          </Text>
          <Text style={[styles.rangeText, { color: COLORS.textSoft }]}>
            {max}{unit}
          </Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    flex: 1,
  },
  valueContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  valueInput: {
    fontSize: 14,
    fontWeight: '700',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 6,
    minWidth: 50,
    textAlign: 'center',
  },
  unit: {
    fontSize: 14,
    fontWeight: '600',
    marginLeft: 2,
  },
  description: {
    fontSize: 12,
    marginBottom: 8,
  },
  sliderContainer: {
    height: 28,
    borderRadius: 14,
    marginTop: 12,
    marginBottom: 8,
    justifyContent: 'center',
    overflow: 'visible',
  },
  progressFill: {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    borderRadius: 14,
  },
  thumb: {
    position: 'absolute',
    width: THUMB_SIZE,
    height: THUMB_SIZE,
    borderRadius: THUMB_SIZE / 2,
    borderWidth: 3,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 4,
    elevation: 5,
  },
  thumbInner: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: 'rgba(255,255,255,0.5)',
  },
  rangeContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 4,
  },
  rangeText: {
    fontSize: 11,
  },
});

export default SettingSlider;
