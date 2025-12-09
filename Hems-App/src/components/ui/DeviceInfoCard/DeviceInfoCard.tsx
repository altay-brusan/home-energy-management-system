import React from 'react';
import { View, Text, StyleSheet, Pressable } from 'react-native';
import { StatusDot, StatusDotStatus } from '../StatusDot';
import { getThemeColors } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export interface DeviceInfoCardProps {
  title: string;
  subtitle?: string;
  icon?: React.ReactNode;
  status: StatusDotStatus;
  onPress?: () => void;
  style?: any;
}

export const DeviceInfoCard: React.FC<DeviceInfoCardProps> = ({
  title,
  subtitle,
  icon,
  status,
  onPress,
  style,
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
        style,
      ]}
    >
      {/* Status Dot - Top Right */}
      <View style={styles.statusContainer}>
        <StatusDot status={status} size={10} />
      </View>

      {/* Content */}
      <View style={styles.content}>
        {/* Icon */}
        {icon && <View style={styles.iconContainer}>{icon}</View>}

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
          {subtitle && (
            <Text
              style={[
                styles.subtitle,
                {
                  color: COLORS.textSoft,
                },
              ]}
              numberOfLines={2}
            >
              {subtitle}
            </Text>
          )}
        </View>
      </View>
    </Pressable>
  );
};

const styles = StyleSheet.create({
  container: {
    borderRadius: 12,
    borderWidth: 0,
    padding: 16,
    minHeight: 100,
  },
  statusContainer: {
    position: 'absolute',
    top: 12,
    right: 12,
  },
  content: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
  },
  iconContainer: {
    marginRight: 12,
  },
  textContainer: {
    flex: 1,
    justifyContent: 'center',
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 11,
    lineHeight: 15,
  },
});
