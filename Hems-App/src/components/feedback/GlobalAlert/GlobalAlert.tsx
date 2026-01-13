import React, { useEffect, useRef, useMemo } from 'react';
import { Animated, Pressable, Text, View, StyleSheet } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { AlertTriangle, AlertCircle, Bell, Info, X } from 'lucide-react-native';
import { useAlertStore } from '../../../store/alertStore';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useTranslation } from 'react-i18next';

const getSeverityConfig = (level: 'info' | 'warning' | 'critical') => {
  switch (level) {
    case 'critical':
      return {
        color: '#EF4444',
        bgColor: 'rgba(239, 68, 68, 0.1)',
        borderColor: 'rgba(239, 68, 68, 0.3)',
        icon: AlertCircle,
        label: 'Critical',
      };
    case 'warning':
      return {
        color: '#F59E0B',
        bgColor: 'rgba(245, 158, 11, 0.1)',
        borderColor: 'rgba(245, 158, 11, 0.3)',
        icon: AlertTriangle,
        label: 'Warning',
      };
    default:
      return {
        color: '#3B82F6',
        bgColor: 'rgba(59, 130, 246, 0.1)',
        borderColor: 'rgba(59, 130, 246, 0.3)',
        icon: Info,
        label: 'Info',
      };
  }
};

export default function GlobalAlert() {
  const { t } = useTranslation();
  const visible = useAlertStore(s => s.visible);
  const title = useAlertStore(s => s.title);
  const message = useAlertStore(s => s.message);
  const hide = useAlertStore(s => s.hide);
  const level = useAlertStore(s => s.level);
  const deviceName = useAlertStore(s => s.deviceName);
  const timestamp = useAlertStore(s => s.timestamp);
  const insets = useSafeAreaInsets();

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => makeStyles(isDark, COLORS), [isDark, COLORS]);

  const anim = useRef(new Animated.Value(0)).current;
  const severityConfig = getSeverityConfig(level);
  const SeverityIcon = severityConfig.icon;

  const formattedTime = useMemo(() => {
    if (!timestamp) return '';
    const d = new Date(timestamp);
    const pad = (n: number) => (n < 10 ? `0${n}` : `${n}`);
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }, [timestamp]);

  useEffect(() => {
    Animated.timing(anim, {
      toValue: visible ? 1 : 0,
      duration: 200,
      useNativeDriver: true,
    }).start();
  }, [visible, anim]);

  if (!visible) return null;

  return (
    <Animated.View style={[styles.overlay, { opacity: anim }]}>
      <Pressable style={{ flex: 1 }} onPress={hide} />
      <Animated.View
        style={[
          styles.sheet,
          {
            paddingBottom: insets.bottom + 20,
            transform: [
              {
                translateY: anim.interpolate({
                  inputRange: [0, 1],
                  outputRange: [300, 0],
                }),
              },
            ],
          },
        ]}
      >
        {/* Accent Bar */}
        <View style={[styles.accentBar, { backgroundColor: severityConfig.color }]} />

        {/* Handle */}
        <View style={styles.handle} />

        {/* Header Row */}
        <View style={styles.headerRow}>
          {/* Severity Badge */}
          <View style={[
            styles.severityBadge,
            { backgroundColor: severityConfig.bgColor, borderColor: severityConfig.borderColor }
          ]}>
            <SeverityIcon size={16} color={severityConfig.color} />
            <Text style={[styles.severityText, { color: severityConfig.color }]}>
              {severityConfig.label}
            </Text>
          </View>

          {/* Close Button */}
          <Pressable onPress={hide} style={styles.closeButton}>
            <X size={20} color={COLORS.textSoft} />
          </Pressable>
        </View>

        {/* Title */}
        {title && (
          <View style={styles.titleContainer}>
            <Text style={styles.title} numberOfLines={2}>{title}</Text>
          </View>
        )}

        {/* Message */}
        {message && (
          <View style={styles.messageCard}>
            <Text style={styles.messageText}>{message}</Text>
          </View>
        )}

        {/* Meta Info */}
        <View style={styles.metaContainer}>
          {/* Device Name */}
          {deviceName && (
            <View style={styles.metaItem}>
              <Text style={styles.metaLabel}>{t('notifications.sourceDevice') || 'Device'}</Text>
              <Text style={styles.metaValue}>{deviceName}</Text>
            </View>
          )}

          {/* Timestamp */}
          {formattedTime && (
            <View style={styles.metaItem}>
              <Text style={styles.metaLabel}>{t('notifications.timestamp') || 'Time'}</Text>
              <Text style={styles.metaValue}>{formattedTime}</Text>
            </View>
          )}
        </View>

        {/* Close Button */}
        <Pressable
          style={[styles.actionButton, { backgroundColor: severityConfig.color }]}
          onPress={hide}
        >
          <Text style={styles.actionButtonText}>{t('globalAlert.close') || 'Close'}</Text>
        </Pressable>
      </Animated.View>
    </Animated.View>
  );
}

const makeStyles = (isDark: boolean, COLORS: any) => {
  const APP_BG = isDark ? '#1c222b' : '#F2F4F7';
  const TEXT = isDark ? '#E6EAF2' : '#111827';
  const MUTED = isDark ? 'rgba(230,234,242,0.64)' : '#6B7280';
  const SURFACE = isDark ? '#252d3a' : '#FFFFFF';

  return StyleSheet.create({
    overlay: {
      position: 'absolute',
      left: 0,
      right: 0,
      top: 0,
      bottom: 0,
      zIndex: 999,
      backgroundColor: 'rgba(0,0,0,0.5)',
      justifyContent: 'flex-end',
    },
    sheet: {
      backgroundColor: APP_BG,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      padding: 20,
      paddingTop: 12,
      shadowColor: '#000',
      shadowOffset: { width: 0, height: -4 },
      shadowOpacity: 0.15,
      shadowRadius: 12,
      elevation: 12,
      position: 'relative',
      overflow: 'hidden',
    },
    accentBar: {
      position: 'absolute',
      left: 0,
      top: 0,
      bottom: 0,
      width: 4,
    },
    handle: {
      alignSelf: 'center',
      width: 40,
      height: 4,
      borderRadius: 2,
      backgroundColor: isDark ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.1)',
      marginBottom: 16,
    },
    headerRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 16,
    },
    severityBadge: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 6,
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 16,
      borderWidth: 1,
    },
    severityText: {
      fontSize: 12,
      fontWeight: '700',
      textTransform: 'uppercase',
      letterSpacing: 0.5,
    },
    closeButton: {
      padding: 8,
      borderRadius: 20,
      backgroundColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.05)',
    },
    titleContainer: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 14,
      marginBottom: 12,
    },
    title: {
      fontSize: 16,
      fontWeight: '700',
      color: TEXT,
      letterSpacing: -0.3,
    },
    messageCard: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 14,
      marginBottom: 12,
    },
    messageText: {
      fontSize: 14,
      fontWeight: '400',
      color: TEXT,
      lineHeight: 22,
    },
    metaContainer: {
      gap: 8,
      marginBottom: 16,
    },
    metaItem: {
      backgroundColor: SURFACE,
      borderRadius: 12,
      padding: 14,
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
    },
    metaLabel: {
      fontSize: 13,
      fontWeight: '500',
      color: MUTED,
    },
    metaValue: {
      fontSize: 13,
      fontWeight: '600',
      color: TEXT,
    },
    actionButton: {
      borderRadius: 12,
      paddingVertical: 14,
      alignItems: 'center',
      justifyContent: 'center',
    },
    actionButtonText: {
      color: '#FFFFFF',
      fontSize: 15,
      fontWeight: '700',
    },
  });
};
