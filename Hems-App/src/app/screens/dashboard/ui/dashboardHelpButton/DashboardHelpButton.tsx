import React from 'react';
import { View } from 'react-native';
import Animated, { FadeIn } from 'react-native-reanimated';
import { getDashboardHelpButtonStyles } from './DashboardHelpButton.styles';
import type { DashboardHelpButtonProps } from './DashboardHelpButton.types';
import { Button } from '../../../../../components';

export default function DashboardHelpButton({
  COLORS,
  onPress,
}: DashboardHelpButtonProps) {
  const styles = getDashboardHelpButtonStyles(COLORS);
  return (
    <Animated.View
      entering={FadeIn.delay(700).duration(600)}
      style={styles.menuRow}
    >
      <Button
        title="Help"
        variant="secondary"
        style={styles.menuBtn}
        textStyle={styles.menuBtnText}
        onPress={onPress}
      />
    </Animated.View>
  );
}
