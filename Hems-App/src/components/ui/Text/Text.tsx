import React from 'react';
import { Text as RNText, TextProps, StyleSheet } from 'react-native';
import { FONTS } from '../../../theme/constants';

/**
 * Custom Text component with Montserrat font family applied by default
 */
export const Text: React.FC<TextProps> = ({ style, ...props }) => {
  return <RNText style={[styles.defaultText, style]} {...props} />;
};

const styles = StyleSheet.create({
  defaultText: {
    fontFamily: FONTS.regular,
  },
});

export default Text;
