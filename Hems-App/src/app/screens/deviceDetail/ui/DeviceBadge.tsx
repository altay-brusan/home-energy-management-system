import React from 'react';
import { Pressable, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { badgeStyles } from '../DeviceDetail.styles';

type Props = {
  COLORS: Record<string, string>;
  title: string;
  subtitle?: string;
  value: string;
  icon?: string;
  status?: 'normal' | 'warning' | 'error' | 'inactive';
  align?: 'left' | 'right';
  onPress: () => void;
};

export default function DeviceBadge({
  COLORS,
  title,
  subtitle,
  value,
  icon,
  status = 'normal',
  align = 'left',
  onPress,
}: Props) {
  const getStatusColor = () => {
    switch (status) {
      case 'warning':
        return COLORS.warning;
      case 'error':
        return COLORS.error;
      case 'inactive':
        return COLORS.slate400;
      default:
        return COLORS.success;
    }
  };

  return (
    <Pressable
      onPress={onPress}
      style={[
        badgeStyles.wrap,
        {
          backgroundColor: COLORS.cardBg,
          borderColor: COLORS.border,
        },
      ]}
    >
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
        <Text style={[badgeStyles.title, { color: COLORS.textSoft }]} numberOfLines={1}>
          {title}
        </Text>
        {icon && (
          <FontAwesome name={icon as any} size={16} color={getStatusColor()} />
        )}
      </View>
      <View style={{ flexDirection: 'row', alignItems: 'baseline', gap: 4 }}>
        <Text style={[badgeStyles.value, { color: getStatusColor() }]} numberOfLines={1}>
          {value}
        </Text>
        {subtitle && (
          <Text style={[badgeStyles.subtitle, { color: COLORS.slate400, opacity: 0.6 }]} numberOfLines={1}>
            {subtitle}
          </Text>
        )}
      </View>
    </Pressable>
  );
}

