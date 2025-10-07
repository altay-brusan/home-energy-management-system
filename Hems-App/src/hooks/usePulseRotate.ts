import { useSharedValue, useAnimatedStyle, withSpring, withTiming } from 'react-native-reanimated';

export function usePulseRotate() {
  const scale = useSharedValue(1);
  const rotation = useSharedValue(0);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scale.value }, { rotateZ: `${rotation.value}deg` }],
  }));

  const onPressFancy = () => {
    scale.value = withSpring(1.03, {}, () => (scale.value = withSpring(1)));
    rotation.value = withTiming(360, { duration: 420 }, () => (rotation.value = 0));
  };

  return { animatedStyle, onPressFancy };
}
