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
      backgroundColor: COLORS.dark,
      borderColor: COLORS.border,
      alignItems: 'flex-start', 
    },
  ]}
>
      <View style={{ flexDirection: 'row', alignItems: 'center' }}>
        <View style={[badgeStyles.dot, { backgroundColor: getStatusColor() }]} />
        <View>
          <View style={{ flexDirection: 'row', alignItems: 'baseline' }}>
            <Text style={[badgeStyles.title, { color: COLORS.textSoft }]} numberOfLines={1}>
              {title}
            </Text>
            {subtitle && (
              <Text style={[badgeStyles.subtitle, { color: COLORS.slate400 }]} numberOfLines={1}>
                {' '}
                {subtitle}
              </Text>
            )}
          </View>
          <Text style={[badgeStyles.value, { color: COLORS.textSoft }]} numberOfLines={1}>
            {value}
          </Text>
        </View>
      </View>
      <View style={badgeStyles.iconContainer}>
        {icon && (
          <FontAwesome name={icon as any} size={14} color={getStatusColor()} style={{ marginRight: 4 }} />
        )}
        <FontAwesome name="chevron-right" size={12} color={COLORS.slate400} />
      </View>
    </Pressable>
  );
}

