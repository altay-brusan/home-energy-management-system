import React from 'react';
import { View, Text, StyleSheet, Pressable } from 'react-native';
import { ChevronRight } from 'lucide-react-native';
import { getThemeColors } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export interface ConnectedDeviceCardProps {
  title: string;
  description?: string;
  icon: React.ReactNode;
  iconBackgroundColor?: string;
  onPress?: () => void;
}

export const ConnectedDeviceCard: React.FC<ConnectedDeviceCardProps> = ({
  title,
  description,
  icon,
  iconBackgroundColor = '#FEF3C7', // Default yellow/light color
  onPress,
}) => {
  const isDark = useThemeStore(state => state.mode) === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.container,
        {
          backgroundColor: COLORS.surface,
          borderColor: COLORS.border,
          opacity: pressed ? 0.7 : 1,
        },
      ]}
    >
      {/* Icon Container */}
      <View
        style={[
          styles.iconWrapper,
          {
            backgroundColor: iconBackgroundColor,
          },
        ]}
      >
        {icon}
      </View>

      {/* Text Content */}
      <View style={styles.textContainer}>
        <Text
          style={[
            styles.title,
            {
              color: COLORS.text,
            },
          ]}
          numberOfLines={1}
        >
          {title}
        </Text>
        {description && (
          <Text
            style={[
              styles.description,
              {
                color: COLORS.textSoft,
              },
            ]}
            numberOfLines={1}
          >
            {description}
          </Text>
        )}
      </View>

      {/* Chevron Icon */}
      <View style={styles.chevronContainer}>
        <ChevronRight size={20} color={COLORS.textSoft} strokeWidth={2} />
      </View>
    </Pressable>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 12,
    borderWidth: 0,
    padding: 16,
    minHeight: 80,
    width: '100%',
  },
  iconWrapper: {
    width: 56,
    height: 56,
    borderRadius: 28,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  textContainer: {
    flex: 1,
    justifyContent: 'center',
  },
  title: {
    fontSize: 15,
    fontWeight: '600',
    marginBottom: 4,
  },
  description: {
    fontSize: 12,
    lineHeight: 16,
  },
  chevronContainer: {
    marginLeft: 8,
  },
});
