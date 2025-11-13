import { StyleSheet } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeNotificationDetailStyles = (isDark: boolean) => {
  const COLORS = getThemeColors(isDark) as any;

  const APP_BG = COLORS?.appBg ?? (isDark ? '#1c222b' : '#F2F4F7');
  const TEXT = COLORS?.text ?? (isDark ? '#E6EAF2' : '#111827');
  const MUTED = COLORS?.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280');
  const ACCENT = COLORS?.accent ?? '#1DD1A1';

  return StyleSheet.create({
    screen: {
      flex: 1,
      backgroundColor: APP_BG,
    },

    // Header
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'flex-start',
      paddingHorizontal: 16,
      paddingVertical: 4,
      marginBottom: 16,
    },
    headerLeft: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 16,
    },
    backButton: {
      paddingVertical: 8,
      paddingRight: 8,
      marginLeft: -4,
    },
    headerTitle: {
      fontSize: 26,
      fontWeight: '700',
      color: TEXT,
      letterSpacing: -0.5,
    },

    // ScrollView
    scrollView: {
      flex: 1,
    },
    scrollContent: {
      padding: 24,
    },

    // Title Container
    titleContainer: {
      marginBottom: 12,
    },
    title: {
      fontSize: 28,
      fontWeight: '700',
      color: TEXT,
      lineHeight: 36,
      letterSpacing: -0.6,
      marginBottom: 8,
    },

    // Critical Badge
    criticalBadge: {
      alignSelf: 'flex-start',
      paddingHorizontal: 10,
      paddingVertical: 4,
      borderRadius: 6,
      marginTop: 4,
    },
    criticalBadgeText: {
      fontSize: 11,
      fontWeight: '700',
      letterSpacing: 0.5,
      textTransform: 'uppercase',
    },

    // Timestamp - Bottom Center Pill
    timestamp: {
      fontSize: 13,
      fontWeight: '500',
      color: MUTED,
      letterSpacing: -0.1,
      textAlign: 'center',
      marginTop: 32,
      alignSelf: 'center',
      paddingHorizontal: 16,
      paddingVertical: 8,
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.04)',
      borderRadius: 999,
      overflow: 'hidden',
    },

    // Message
    message: {
      fontSize: 17,
      fontWeight: '400',
      color: TEXT,
      lineHeight: 26,
      letterSpacing: -0.3,
      marginTop: 24,
      marginBottom: 24,
    },

    // Info Card - Modern Accent Design
    infoCard: {
      backgroundColor: isDark ? 'rgba(29,209,161,0.12)' : 'rgba(29,209,161,0.08)',
      borderRadius: 12,
      padding: 20,
      borderWidth: 1,
      borderColor: isDark ? 'rgba(29,209,161,0.2)' : 'rgba(29,209,161,0.15)',
    },
    infoHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 10,
      marginBottom: 12,
      paddingBottom: 12,
      borderBottomWidth: 1,
      borderBottomColor: isDark ? 'rgba(29,209,161,0.15)' : 'rgba(29,209,161,0.12)',
    },
    infoIcon: {
      width: 32,
      height: 32,
      borderRadius: 8,
      backgroundColor: isDark ? 'rgba(29,209,161,0.2)' : 'rgba(29,209,161,0.15)',
      alignItems: 'center',
      justifyContent: 'center',
    },
    infoTitle: {
      fontSize: 15,
      fontWeight: '700',
      color: ACCENT,
      letterSpacing: -0.3,
      flex: 1,
    },
    infoText: {
      fontSize: 15,
      fontWeight: '500',
      color: isDark ? TEXT : '#374151',
      lineHeight: 22,
      letterSpacing: -0.2,
    },
  });
};
