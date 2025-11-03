import { StyleSheet } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeNotificationDetailStyles = (isDark: boolean) => {
  const COLORS = getThemeColors(isDark) as any;

  const APP_BG = isDark ? '#1C222B' : '#F5F7FB';
  const CARD_BG = isDark ? '#272E38' : '#F3F4F6';
  const TEXT = COLORS?.text ?? (isDark ? '#E6EAF2' : '#111827');
  const MUTED = COLORS?.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280');

  return StyleSheet.create({
    container: {
      flex: 1,
      alignItems: 'flex-start',
      padding: 16,
      backgroundColor: APP_BG,
    },
    pageTitleRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      width: '100%',
      marginBottom: 20,
      paddingHorizontal: 8,
    },
    pageTitle: {
      fontSize: 31,
      fontWeight: '800',
      color: TEXT,
    },
    card: {
      width: '100%',
      maxWidth: 420,
      borderRadius: 20,
      marginBottom: 15,
      backgroundColor: CARD_BG,
      padding: 28,
    },
    itemTitle: {
      fontSize: 22,
      fontWeight: '700',
      color: TEXT,
      marginBottom: 12,
    },
    divider: {
      height: 1,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : '#D1D5DB',
      marginVertical: 16,
    },
    description: {
      fontSize: 20,
      fontWeight: '500',
      color: TEXT,
      lineHeight: 30,
      marginBottom: 20,
    },
    footerRow: {
      flexDirection: 'row',
      justifyContent: 'flex-end',
      alignItems: 'center',
      marginTop: 16,
    },
    badge: {
      paddingHorizontal: 10,
      paddingVertical: 5,
      borderRadius: 8,
    },
    badgeText: {
      fontSize: 13,
      fontWeight: '700',
    },
    time: {
      fontSize: 15,
      fontWeight: '600',
      color: '#009687ff',
    },
    button: {
      marginTop: 16,
      height: 56,
      borderRadius: 16,
      backgroundColor: COLORS.buttonBg,
      borderWidth: 1,
      borderColor: COLORS.border,
      width: '100%',
    },
    buttonText: {
      fontSize: 16,
      fontWeight: '700',
      letterSpacing: 0.3,
      color: isDark ? COLORS.textSoft : '#111827',
      textAlign: 'center',
    },
    additionalDataContainer: {
      backgroundColor: isDark ? 'rgba(29,209,161,0.08)' : 'rgba(29,209,161,0.06)',
      borderRadius: 12,
      padding: 16,
      marginTop: 12,
      marginBottom: 16,
      borderLeftWidth: 4,
      borderLeftColor: COLORS.accent || '#1DD1A1',
    },
    additionalDataTitleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 8,
    },
    additionalDataTitle: {
      fontSize: 16,
      fontWeight: '700',
      color: TEXT,
      marginLeft: 8,
    },
    additionalDataText: {
      fontSize: 15,
      fontWeight: '500',
      color: isDark ? '#E6EAF2' : '#374151',
      fontFamily: 'monospace',
      lineHeight: 22,
    },
  });
};
