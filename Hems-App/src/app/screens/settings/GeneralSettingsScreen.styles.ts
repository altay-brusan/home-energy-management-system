import { StyleSheet } from 'react-native';

export const makeGeneralSettingsScreenStyles = (COLORS: Record<string, string>, isDark: boolean) => {
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
    settingCard: {
      borderRadius: 12,
      marginBottom: 12,
      padding: 16,
    },
    settingRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      flexWrap: 'wrap',
      gap: 12,
    },
    settingRowNav: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
    },
    settingLabelRow: {
      flexDirection: 'row',
      alignItems: 'center',
      flex: 1,
      minWidth: 120,
    },
    settingLabel: {
      fontSize: 15,
      fontWeight: '500',
    },
  });
};
