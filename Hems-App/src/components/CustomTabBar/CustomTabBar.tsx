import React from 'react';
import { View, TouchableOpacity, Text, StyleSheet, Platform } from 'react-native';
import { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getDesignTokens } from '../../theme/constants';
import { useThemeStore } from '../../store/themeStore';

export function CustomTabBar({ state, descriptors, navigation }: BottomTabBarProps) {
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return (
    <View style={[styles.container, {
      paddingBottom: Platform.OS === 'ios' ? insets.bottom : 12,
      backgroundColor: tokens.surface.base,
    }]}>
      <View style={[styles.tabBar, {
        backgroundColor: tokens.surface.base,
        borderTopColor: tokens.border.primary,
      }]}>
        {state.routes.map((route, index) => {
          const { options } = descriptors[route.key];
          const label =
            options.tabBarLabel !== undefined
              ? options.tabBarLabel
              : options.title !== undefined
              ? options.title
              : route.name;

          const isFocused = state.index === index;

          const onPress = () => {
            const event = navigation.emit({
              type: 'tabPress',
              target: route.key,
              canPreventDefault: true,
            });

            if (!isFocused && !event.defaultPrevented) {
              navigation.navigate(route.name, route.params);
            }
          };

          const onLongPress = () => {
            navigation.emit({
              type: 'tabLongPress',
              target: route.key,
            });
          };

          // Get colors based on focus state
          const iconColor = isFocused ? tokens.brand.accent : tokens.text.tertiary;
          const textColor = isFocused ? tokens.brand.accent : tokens.text.tertiary;

          // Get icon component
          const iconComponent = options.tabBarIcon?.({
            focused: isFocused,
            color: iconColor,
            size: 22,
          });

          return (
            <TouchableOpacity
              key={route.key}
              accessibilityRole="button"
              accessibilityState={isFocused ? { selected: true } : {}}
              accessibilityLabel={options.tabBarAccessibilityLabel}
              testID={options.tabBarTestID}
              onPress={onPress}
              onLongPress={onLongPress}
              activeOpacity={0.7}
              style={styles.tabButton}
            >
              <View style={styles.iconContainer}>
                {iconComponent}
              </View>
              <Text
                style={[
                  styles.label,
                  { color: textColor },
                ]}
                numberOfLines={1}
              >
                {label as string}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
  },
  tabBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-around',
    paddingTop: 8,
    paddingHorizontal: 8,
    borderTopWidth: 1,
  },
  tabButton: {
    flex: 1,
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 4,
    gap: 4,
  },
  iconContainer: {
    justifyContent: 'center',
    alignItems: 'center',
    height: 24,
  },
  label: {
    fontSize: 10,
    fontWeight: '500',
    textAlign: 'center',
  },
});
