import React, { useEffect, useRef } from 'react';
import { Animated, Dimensions, View, StyleSheet } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';

export const DeviceDetailSkeleton: React.FC = () => {
  const isDark = useThemeStore((s) => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const shimmerAnim = useRef(new Animated.Value(0)).current;

  const width = Dimensions.get('window').width;
  const isSmall = width <= 380;

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

  return (
    <View style={styles.container}>
      <View style={{ paddingTop: insets.top + 80, paddingHorizontal: isSmall ? SPACING.sm : SPACING.md }}>
        {/* Top Section Skeleton */}
        <View style={[styles.topSection, { flexDirection: isSmall ? 'column' : 'row', gap: SPACING.md }]}>
          {/* Device Image Skeleton */}
          <View
            style={{
              flex: isSmall ? 1 : 0.45,
              backgroundColor: COLORS.surface,
              borderRadius: 16,
              padding: SPACING.md,
              borderWidth: 0,
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: isSmall ? 180 : 220,
            }}
          >
            <SkeletonBox width="70%" height={isSmall ? 140 : 180} style={{ borderRadius: 12 }} />
          </View>

          {/* Info Cards Skeleton */}
          <View style={{ flex: isSmall ? 1 : 0.52, gap: SPACING.sm, justifyContent: 'space-between' }}>
            {/* Inverter Card Skeleton - Gerçek DeviceInfoCard boyutları */}
            <View
              style={{
                flex: 1,
                backgroundColor: COLORS.surface,
                borderRadius: 12,
                padding: 16,
                minHeight: 100,
                borderWidth: 0,
                justifyContent: 'center',
              }}
            >
              <View style={{ flexDirection: 'row', alignItems: 'center' }}>
                {/* Icon */}
                <SkeletonBox width={24} height={24} style={{ borderRadius: 6, marginRight: 12 }} />
                {/* Text Content */}
                <View style={{ flex: 1 }}>
                  <SkeletonBox width="65%" height={16} style={{ marginBottom: 4, borderRadius: 4 }} />
                  <SkeletonBox width="85%" height={13} style={{ borderRadius: 4 }} />
                </View>
              </View>
              {/* Status Dot - Top Right */}
              <View style={{ position: 'absolute', top: 12, right: 12 }}>
                <SkeletonBox width={10} height={10} style={{ borderRadius: 5 }} />
              </View>
            </View>

            {/* Battery Card Skeleton - Gerçek DeviceInfoCard boyutları */}
            <View
              style={{
                flex: 1,
                backgroundColor: COLORS.surface,
                borderRadius: 12,
                padding: 16,
                minHeight: 100,
                borderWidth: 0,
                justifyContent: 'center',
              }}
            >
              <View style={{ flexDirection: 'row', alignItems: 'center' }}>
                {/* Icon */}
                <SkeletonBox width={24} height={24} style={{ borderRadius: 6, marginRight: 12 }} />
                {/* Text Content */}
                <View style={{ flex: 1 }}>
                  <SkeletonBox width="50%" height={16} style={{ marginBottom: 4, borderRadius: 4 }} />
                  <SkeletonBox width="70%" height={13} style={{ borderRadius: 4 }} />
                </View>
              </View>
              {/* Status Dot - Top Right */}
              <View style={{ position: 'absolute', top: 12, right: 12 }}>
                <SkeletonBox width={10} height={10} style={{ borderRadius: 5 }} />
              </View>
            </View>
          </View>
        </View>

        {/* Connected Devices Section Skeleton */}
        <View style={{ marginTop: SPACING.sm }}>
          <SkeletonBox width={180} height={20} style={{ marginBottom: SPACING.sm, marginLeft: 4, borderRadius: 6 }} />

          <View style={{ gap: SPACING.sm }}>
            {/* Solar Panel Card Skeleton - Yeni horizontal layout */}
            <View
              style={{
                width: '100%',
                minHeight: 80,
                backgroundColor: COLORS.surface,
                borderRadius: 12,
                padding: 16,
                borderWidth: 0,
                flexDirection: 'row',
                alignItems: 'center',
              }}
            >
              {/* Icon wrapper - Sol tarafta */}
              <SkeletonBox width={56} height={56} style={{ borderRadius: 28, marginRight: 12 }} />
              {/* Text content - Ortada */}
              <View style={{ flex: 1 }}>
                <SkeletonBox width={120} height={15} style={{ marginBottom: 4, borderRadius: 4 }} />
                <SkeletonBox width="80%" height={12} style={{ borderRadius: 4 }} />
              </View>
              {/* Chevron - Sağda */}
              <SkeletonBox width={20} height={20} style={{ borderRadius: 4, marginLeft: 8 }} />
            </View>

            {/* Gateway Card Skeleton - Yeni horizontal layout */}
            <View
              style={{
                width: '100%',
                minHeight: 80,
                backgroundColor: COLORS.surface,
                borderRadius: 12,
                padding: 16,
                borderWidth: 0,
                flexDirection: 'row',
                alignItems: 'center',
              }}
            >
              <SkeletonBox width={56} height={56} style={{ borderRadius: 28, marginRight: 12 }} />
              <View style={{ flex: 1 }}>
                <SkeletonBox width={130} height={15} style={{ marginBottom: 4, borderRadius: 4 }} />
                <SkeletonBox width="75%" height={12} style={{ borderRadius: 4 }} />
              </View>
              <SkeletonBox width={20} height={20} style={{ borderRadius: 4, marginLeft: 8 }} />
            </View>

            {/* Smart Meter Card Skeleton - Yeni horizontal layout */}
            <View
              style={{
                width: '100%',
                minHeight: 80,
                backgroundColor: COLORS.surface,
                borderRadius: 12,
                padding: 16,
                borderWidth: 0,
                flexDirection: 'row',
                alignItems: 'center',
              }}
            >
              <SkeletonBox width={56} height={56} style={{ borderRadius: 28, marginRight: 12 }} />
              <View style={{ flex: 1 }}>
                <SkeletonBox width={100} height={15} style={{ marginBottom: 4, borderRadius: 4 }} />
                <SkeletonBox width="85%" height={12} style={{ borderRadius: 4 }} />
              </View>
              <SkeletonBox width={20} height={20} style={{ borderRadius: 4, marginLeft: 8 }} />
            </View>
          </View>
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  topSection: {
    marginBottom: SPACING.md,
    alignItems: 'stretch',
  },
});
