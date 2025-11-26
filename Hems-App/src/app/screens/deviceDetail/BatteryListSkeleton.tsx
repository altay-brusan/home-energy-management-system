import React, { useEffect, useRef } from 'react';
import { Animated, View, StyleSheet } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';

export const BatteryListSkeleton: React.FC = () => {
  const isDark = useThemeStore((s) => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const shimmerAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const shimmer = Animated.loop(
      Animated.sequence([
        Animated.timing(shimmerAnim, {
          toValue: 1,
          duration: 1200,
          useNativeDriver: true,
        }),
        Animated.timing(shimmerAnim, {
          toValue: 0,
          duration: 1200,
          useNativeDriver: true,
        }),
      ])
    );
    shimmer.start();
    return () => shimmer.stop();
  }, []);

  const opacity = shimmerAnim.interpolate({
    inputRange: [0, 1],
    outputRange: [0.3, 0.7],
  });

  const SkeletonBox = ({ width, height, style }: any) => (
    <Animated.View
      style={[
        {
          width,
          height,
          backgroundColor: isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.08)',
          borderRadius: 8,
          opacity,
        },
        style,
      ]}
    />
  );

  const BatteryCardSkeleton = () => (
    <View
      style={{
        backgroundColor: COLORS.surface,
        borderRadius: 14,
        padding: SPACING.md,
        marginBottom: SPACING.sm,
        flexDirection: 'row',
        alignItems: 'center',
      }}
    >
      {/* Icon */}
      <SkeletonBox width={56} height={56} style={{ borderRadius: 28, marginRight: SPACING.sm }} />

      {/* Content */}
      <View style={{ flex: 1 }}>
        <SkeletonBox width="60%" height={16} style={{ marginBottom: 6, borderRadius: 4 }} />
        <SkeletonBox width="80%" height={12} style={{ marginBottom: 6, borderRadius: 4 }} />
        <SkeletonBox width="70%" height={12} style={{ borderRadius: 4 }} />
      </View>

      {/* SOC Badge */}
      <SkeletonBox width={60} height={32} style={{ borderRadius: 12 }} />
    </View>
  );

  return (
    <View style={styles.container}>
      <View style={{ padding: SPACING.md }}>
        {/* 3 Battery Card Skeleton */}
        <BatteryCardSkeleton />
        <BatteryCardSkeleton />
        <BatteryCardSkeleton />
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});
