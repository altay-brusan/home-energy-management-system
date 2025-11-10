import React from 'react';
import { View, TouchableOpacity, Text, StyleSheet, Platform } from 'react-native';
import { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { getThemeColors } from '../../theme/constants';
import { useThemeStore } from '../../store/themeStore';

export function CustomTabBar({ state, descriptors, navigation }: BottomTabBarProps) {
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <View style={[styles.container, {
      paddingBottom: Platform.OS === 'ios' ? insets.bottom : 16,
      backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
    }]}>
      <View style={[styles.tabBar, {
        backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
        borderTopColor: isDark ? COLORS.border : '#E5E7EB',
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

          // Get icon component
          const iconComponent = options.tabBarIcon?.({
            focused: isFocused,
            color: isFocused ? COLORS.accent : (isDark ? COLORS.textSoft : COLORS.slate400),
            size: 24,
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
              style={[
                styles.tabButton,
                isFocused && {
                  backgroundColor: isDark ? COLORS.accent + '30' : COLORS.accent + '20',
                  paddingHorizontal: 16,
                },
              ]}
            >
              <View style={styles.iconContainer}>
                {iconComponent}
              </View>
              {isFocused && (
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.accent,
                    },
                  ]}
                  numberOfLines={1}
                >
                  {label as string}
                </Text>
              )}
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
    justifyContent: 'space-between',
    paddingVertical: 12,
    paddingHorizontal: 16,
    gap: 8,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  tabButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 8,
    borderRadius: 9999, // Fully rounded
    gap: 8,
  },
  iconContainer: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    letterSpacing: 0.2,
  },
});
