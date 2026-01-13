import { StyleSheet } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeNotificationDetailStyles = (isDark: boolean) => {
  const COLORS = getThemeColors(isDark) as any;

  const APP_BG = COLORS?.appBg ?? (isDark ? '#1c222b' : '#F2F4F7');
  const TEXT = COLORS?.text ?? (isDark ? '#E6EAF2' : '#111827');
  const MUTED = COLORS?.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280');
  const SURFACE = COLORS?.surface ?? (isDark ? '#252d3a' : '#FFFFFF');

  return StyleSheet.create({
    screen: {
      flex: 1,
      backgroundColor: APP_BG,
    },

    // Header (Settings format)
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 12,
      marginBottom: 8,
    },
    headerBackButton: {
      padding: 8,
      marginLeft: -8,
    },
    headerTitle: {
      fontSize: 18,
      fontWeight: '600',
      color: TEXT,
      flex: 1,
      textAlign: 'left',
      marginLeft: 8,
    },
    headerRight: {
      width: 40,
    },

    // ScrollView
    scrollView: {
      flex: 1,
    },
    scrollContent: {
      padding: 20,
      gap: 16,
    },

    // Severity Badge
    severityBadge: {
      flexDirection: 'row',
      alignItems: 'center',
      alignSelf: 'flex-start',
      gap: 8,
      paddingHorizontal: 14,
      paddingVertical: 8,
      borderRadius: 20,
      borderWidth: 1,
    },
    severityText: {
      fontSize: 14,
      fontWeight: '600',
      textTransform: 'uppercase',
      letterSpacing: 0.5,
    },

    // Error Code
    codeContainer: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 16,
      ...(isDark ? {} : {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 1 },
        shadowOpacity: 0.05,
        shadowRadius: 3,
        elevation: 1,
      }),
    },
    codeLabel: {
      fontSize: 12,
      fontWeight: '600',
      color: MUTED,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
      marginBottom: 6,
    },
    codeValue: {
      fontSize: 18,
      fontWeight: '700',
      color: TEXT,
      fontFamily: 'monospace',
      letterSpacing: 1,
    },

    // Message Card
    messageCard: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 16,
      ...(isDark ? {} : {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 1 },
        shadowOpacity: 0.05,
        shadowRadius: 3,
        elevation: 1,
      }),
    },
    messageText: {
      fontSize: 16,
      fontWeight: '400',
      color: TEXT,
      lineHeight: 24,
      letterSpacing: -0.2,
    },

    // Meta Items
    metaItem: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 16,
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      ...(isDark ? {} : {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 1 },
        shadowOpacity: 0.05,
        shadowRadius: 3,
        elevation: 1,
      }),
    },
    metaLabel: {
      fontSize: 14,
      fontWeight: '500',
      color: MUTED,
    },
    metaValue: {
      fontSize: 14,
      fontWeight: '600',
      color: TEXT,
      textTransform: 'capitalize',
    },
  });
};
