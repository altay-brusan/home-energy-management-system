import React from 'react';
import { View } from 'react-native';
import Animated, { FadeIn } from 'react-native-reanimated';
import { getDashboardHelpButtonStyles } from './DashboardHelpButton.styles';
import type { DashboardHelpButtonProps } from './DashboardHelpButton.types';
import { Button } from '../../../../../components';
import { useTranslation } from 'react-i18next';

export default function DashboardHelpButton({
  COLORS,
  onPress,
}: DashboardHelpButtonProps) {
  const styles = getDashboardHelpButtonStyles(COLORS);
    const { t } = useTranslation();
  return (
    <Animated.View
      entering={FadeIn.delay(700).duration(600)}
      style={styles.menuRow}
    >
      <Button
      title={t('dashboardHelpButton.title')}
        variant="secondary"
        style={styles.menuBtn}
        textStyle={styles.menuBtnText}
        onPress={onPress}
      />
    </Animated.View>
  );
}
