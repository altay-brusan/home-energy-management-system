import React from 'react';
import { Pressable, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors, SPACING, TYPOGRAPHY } from '../../../../theme/constants';

type Props = {
  title: string;
  onBack: () => void;
  rightIconName?: string;
  onRightPress?: () => void;
};

export default function DeviceHeader({ title, onBack, rightIconName, onRightPress }: Props) {
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();

  return (
    <View
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        zIndex: 10,
        backgroundColor: COLORS.dark,
        paddingTop: insets.top,
      }}
    >
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'space-between',
          paddingHorizontal: SPACING.md,
          paddingVertical: SPACING.sm,
        }}
      >
        <Pressable
          style={{
            width: 36,
            height: 36,
            borderRadius: 18,
            backgroundColor: COLORS.surface,
            alignItems: 'center',
            justifyContent: 'center',
            borderWidth: 1,
            borderColor: COLORS.border,
          }}
          onPress={onBack}
        >
          <FontAwesome name="chevron-left" size={16} color={COLORS.textSoft} />
        </Pressable>
        <Text
          style={{ fontSize: TYPOGRAPHY.md, fontWeight: '700', color: COLORS.textSoft }}
          numberOfLines={1}
        >
          {title}
        </Text>
        {rightIconName ? (
          <Pressable
            style={{
              width: 36,
              height: 36,
              borderRadius: 18,
              backgroundColor: COLORS.surface,
              alignItems: 'center',
              justifyContent: 'center',
              borderWidth: 1,
              borderColor: COLORS.border,
            }}
            onPress={onRightPress}
          >
            <FontAwesome name={rightIconName as any} size={18} color={COLORS.accent} />
          </Pressable>
        ) : (
          <View style={{ width: 36 }} />
        )}
      </View>
    </View>
  );
}

