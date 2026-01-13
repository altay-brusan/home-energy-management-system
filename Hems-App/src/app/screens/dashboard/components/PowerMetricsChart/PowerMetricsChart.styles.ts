import { StyleSheet } from 'react-native';
import { getDesignTokens } from '../../../../../theme/constants';
import { useThemeStore } from '../../../../../store/themeStore';

export const getStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    container: {
      backgroundColor: tokens.surface.card,
      borderRadius: 12,
      padding: 16,
      marginVertical: 8,
    },
    loadingContainer: {
      height: 400,
      justifyContent: 'center',
      alignItems: 'center',
    },
    errorContainer: {
      height: 200,
      justifyContent: 'center',
      alignItems: 'center',
      padding: 20,
    },
    errorText: {
      color: tokens.semantic.error,
      fontSize: 14,
      textAlign: 'center',
      marginTop: 8,
    },
    chartContainer: {
      marginVertical: 16,
    },
    svgContainer: {
      backgroundColor: tokens.background.tertiary,
      borderRadius: 8,
    },
  });
};

export const getMetricsHeaderStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    container: {
      backgroundColor: tokens.background.secondary,
      borderRadius: 8,
      padding: 12,
      marginBottom: 16,
    },
    timeLabel: {
      color: tokens.text.primary,
      fontSize: 18,
      fontWeight: '600',
      marginBottom: 12,
    },
    metricsGrid: {
      flexDirection: 'column',
      gap: 8,
    },
    metricRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 12,
    },
    metricDot: {
      width: 10,
      height: 10,
      borderRadius: 5,
    },
    metricLabel: {
      color: tokens.text.tertiary,
      fontSize: 13,
      flex: 1,
    },
    metricValue: {
      color: tokens.text.primary,
      fontSize: 14,
      fontWeight: '500',
    },
    loadingText: {
      color: tokens.text.disabled,
      fontSize: 14,
    },
  });
};

export const getChartLegendStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    container: {
      marginTop: 16,
      paddingTop: 12,
      borderTopWidth: 1,
      borderTopColor: tokens.border.light,
    },
    legendGrid: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: 16,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
      minWidth: '45%',
    },
    legendCheckbox: {
      width: 18,
      height: 18,
      borderWidth: 2,
      borderRadius: 3,
      alignItems: 'center',
      justifyContent: 'center',
    },
    legendCheckmark: {
      width: 10,
      height: 10,
      borderRadius: 2,
    },
    legendDot: {
      width: 10,
      height: 10,
      borderRadius: 5,
    },
    legendLabel: {
      color: tokens.text.tertiary,
      fontSize: 12,
    },
  });
};

export const getSunInfoStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    container: {
      flexDirection: 'row',
      justifyContent: 'center',
      alignItems: 'center',
      gap: 24,
      paddingVertical: 12,
      marginTop: 8,
    },
    sunItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
    },
    sunText: {
      color: tokens.text.quaternary,
      fontSize: 12,
    },
    sunTime: {
      color: tokens.text.tertiary,
      fontWeight: '500',
    },
    sunDivider: {
      color: tokens.text.disabled,
    },
  });
};

export const getLineChartStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    svgContainer: {
      backgroundColor: tokens.background.tertiary,
      borderRadius: 8,
    },
  });
};

export const getLegendDynamicStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return {
    legendDotInactive: { backgroundColor: tokens.utility.gray700 },
    legendLabelInactive: { color: tokens.text.disabled },
    legendLabelActive: { color: tokens.text.tertiary },
  };
};

export const getDashboardChartStyles = () => {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    loadingText: {
      color: tokens.text.disabled,
      marginTop: 10,
    },
  });
};