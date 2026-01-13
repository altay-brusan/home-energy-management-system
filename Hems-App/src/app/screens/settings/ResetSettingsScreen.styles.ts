import { StyleSheet } from 'react-native';

export const makeResetSettingsScreenStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  return StyleSheet.create({
    container: {
      flex: 1,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 12,
    },
    backButton: {
      padding: 8,
    },
    title: {
      fontSize: 18,
      fontWeight: '600',
      flex: 1,
      textAlign: 'center',
      marginHorizontal: 8,
    },
    scrollContent: {
      paddingHorizontal: 16,
      paddingTop: 16,
    },
    card: {
      flexDirection: 'row',
      alignItems: 'center',
      padding: 16,
      borderRadius: 12,
      marginBottom: 12,
    },
    iconContainer: {
      width: 44,
      height: 44,
      borderRadius: 22,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: 14,
    },
    cardContent: {
      flex: 1,
    },
    cardTitle: {
      fontSize: 16,
      fontWeight: '600',
      marginBottom: 4,
    },
    cardSubtitle: {
      fontSize: 13,
      lineHeight: 18,
    },
    loadingText: {
      fontSize: 16,
      fontWeight: '600',
    },
    warningBanner: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      padding: 16,
      borderRadius: 12,
      marginTop: 8,
    },
    warningIcon: {
      marginRight: 12,
      marginTop: 2,
    },
    warningText: {
      flex: 1,
      fontSize: 13,
      lineHeight: 18,
    },
  });
};
