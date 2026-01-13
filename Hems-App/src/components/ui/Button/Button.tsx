import React from 'react';
import {
  GestureResponderEvent,
  StyleProp,
  Text,
  TextStyle,
  TouchableOpacity,
  View,
  ViewStyle,
} from 'react-native';
import { getButtonStyles } from './Button.styles';

interface ButtonProps {
  title: string;
  onPress: (event: GestureResponderEvent) => void;
  variant?: 'primary' | 'secondary' | 'danger';
  disabled?: boolean;
  style?: StyleProp<ViewStyle>;
  textStyle?: StyleProp<TextStyle>;
  color?: string;
  leftIcon?: React.ReactNode;
}

export default function Button({
  title,
  onPress,
  variant = 'primary',
  disabled = false,
  style,
  textStyle,
  color,
  leftIcon,
}: ButtonProps) {
  const styles = getButtonStyles();
  const variantTextColor = (styles as any)?.[variant]?.color;
  return (
    <TouchableOpacity
      style={[
        styles.button,
        styles[variant],
        disabled && styles.disabled,
        style,
        {
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'center',
        },
      ]}
      onPress={onPress}
      disabled={disabled}
      activeOpacity={0.7}
    >
      {leftIcon && <View style={{ marginRight: 8 }}>{leftIcon}</View>}
      <Text
        style={[
          styles.text,
          variantTextColor ? { color: variantTextColor } : null,
          color ? { color } : null,
          textStyle,
        ]}
      >
        {title}
      </Text>
    </TouchableOpacity>
  );
}
