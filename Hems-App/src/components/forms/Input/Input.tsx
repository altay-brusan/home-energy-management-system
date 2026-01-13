import React, { useState } from 'react';
import { View, Text, TextInput, TextInputProps } from 'react-native';
import { getInputStyles } from './Input.styles';

interface InputProps extends TextInputProps {
  label?: string;
  error?: string;
}

export default function Input({
  label,
  error,
  style,
  placeholderTextColor,
  onFocus,
  onBlur,
  ...props
}: InputProps) {
  const [focused, setFocused] = useState(false);
  const styles = getInputStyles({ focused, error: !!error });

  const computedPlaceholder =
    placeholderTextColor ?? styles.placeholderColor;

  return (
    <View style={styles.container}>
      {label && <Text style={styles.label}>{label}</Text>}
      <TextInput
        {...props}
        style={[
          styles.input,
          focused && styles.inputFocused,
          !!error && styles.inputError,
          style,
        ]}
        placeholderTextColor={computedPlaceholder}
        onFocus={e => {
          setFocused(true);
          onFocus?.(e);
        }}
        onBlur={e => {
          setFocused(false);
          onBlur?.(e);
        }}
      />
      {!!error && <Text style={styles.error}>{error}</Text>}
    </View>
  );
}
