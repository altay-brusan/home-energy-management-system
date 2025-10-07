import React from 'react';
import { Pressable, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { gridStyles } from '../DeviceDetail.styles';

type Props = {
  COLORS: Record<string, string>;
  title: string;
  subtitle: string;
  icon?: string;
  onPress: () => void;
};

export default function GridCard({ COLORS, title, subtitle, icon, onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      style={[
        gridStyles.card,
        {
          backgroundColor: COLORS.dark,
          borderColor: COLORS.border,
        },
      ]}
    >
      <Text style={[gridStyles.title, { color: COLORS.textSoft }]} numberOfLines={1}>
        {title}
      </Text>
      <Text style={[gridStyles.subtitle, { color: COLORS.slate400 }]} numberOfLines={1}>
        {subtitle}
      </Text>

      {icon && (
        <View style={gridStyles.iconContainer}>
          <FontAwesome name={icon as any} size={22} color={COLORS.accent} />
          <FontAwesome name="chevron-right" size={12} color={COLORS.slate400} style={gridStyles.chevron} />
        </View>
      )}
    </Pressable>
  );
}

