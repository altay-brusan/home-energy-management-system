import React, { JSX, useMemo, useState } from 'react';
import { View, Text, TouchableOpacity, ScrollView, Alert } from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ArrowLeft, AlertTriangle, AlertCircle, Bell, Info, Trash2 } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeNotificationDetailStyles } from './NotificationDetailScreen.styles';
import { useI18n } from '../../../hooks/useI18n';

import { DeviceNotification, NotificationSeverityLevel, useDeviceNotifications } from '../../../hooks/useDeviceNotifications';

type DetailItem = DeviceNotification;

type ParamList = {
  NotificationDetail: { item: DetailItem };
};

const getSeverityConfig = (severityLevel: NotificationSeverityLevel) => {
  switch (severityLevel) {
    case 'ERROR':
      return {
        color: '#EF4444',
        bgColor: 'rgba(239, 68, 68, 0.1)',
        borderColor: 'rgba(239, 68, 68, 0.3)',
        icon: AlertCircle,
        label: 'Error',
      };
    case 'WARNING':
      return {
        color: '#F59E0B',
        bgColor: 'rgba(245, 158, 11, 0.1)',
        borderColor: 'rgba(245, 158, 11, 0.3)',
        icon: AlertTriangle,
        label: 'Warning',
      };
    case 'ALARM':
      return {
        color: '#3B82F6',
        bgColor: 'rgba(59, 130, 246, 0.1)',
        borderColor: 'rgba(59, 130, 246, 0.3)',
        icon: Bell,
        label: 'Alarm',
      };
    default:
      return {
        color: '#1DD1A1',
        bgColor: 'rgba(29, 209, 161, 0.1)',
        borderColor: 'rgba(29, 209, 161, 0.3)',
        icon: Info,
        label: 'Info',
      };
  }
};

export default function NotificationDetailScreen(): JSX.Element {
  const route = useRoute<RouteProp<ParamList, 'NotificationDetail'>>();
  const { item } = route.params;

  const navigation = useNavigation<NativeStackNavigationProp<any>>();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => makeNotificationDetailStyles(isDark), [isDark]);
  const { t } = useI18n();
  const insets = useSafeAreaInsets();

  const { deleteNotification } = useDeviceNotifications();
  const [isDeleting, setIsDeleting] = useState(false);

  const handleDeletePress = () => {
    Alert.alert(
      t('notifications.deleteConfirm.title'),
      t('notifications.deleteConfirm.message'),
      [
        {
          text: t('notifications.deleteConfirm.cancel'),
          style: 'cancel',
        },
        {
          text: t('notifications.deleteConfirm.confirm'),
          style: 'destructive',
          onPress: async () => {
            try {
              setIsDeleting(true);
              await deleteNotification(item.id);
              navigation.goBack();
            } catch (error) {
              console.error('Failed to delete notification:', error);
            } finally {
              setIsDeleting(false);
            }
          },
        },
      ],
      { cancelable: true }
    );
  };

  const severityConfig = getSeverityConfig(item.severityLevel);
  const SeverityIcon = severityConfig.icon;

  return (
    <View style={[styles.screen, { paddingTop: insets.top + 8 }]}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.headerBackButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{t('notifications.details')}</Text>
        <TouchableOpacity
          onPress={handleDeletePress}
          style={styles.headerBackButton}
          disabled={isDeleting}
        >
          <Trash2 size={22} color={isDeleting ? COLORS.textSoft : '#EF4444'} />
        </TouchableOpacity>
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Severity Badge */}
        <View style={[
          styles.severityBadge,
          { backgroundColor: severityConfig.bgColor, borderColor: severityConfig.borderColor }
        ]}>
          <SeverityIcon size={18} color={severityConfig.color} />
          <Text style={[styles.severityText, { color: severityConfig.color }]}>
            {severityConfig.label}
          </Text>
        </View>

        {/* Error Code */}
        {item.resolvedCode && (
          <View style={styles.codeContainer}>
            <Text style={styles.codeLabel}>{t('notifications.errorCode') || 'Error Code'}</Text>
            <Text style={styles.codeValue}>{item.resolvedCode}</Text>
          </View>
        )}

        {/* Display Message */}
        {item.displayMessage && (
          <View style={styles.messageCard}>
            <Text style={styles.messageText}>{item.displayMessage}</Text>
          </View>
        )}

        {/* Fallback to original message if no displayMessage */}
        {!item.displayMessage && item.message && (
          <View style={styles.messageCard}>
            <Text style={styles.messageText}>{item.message}</Text>
          </View>
        )}

        {/* Source Device */}
        {item.sourceDevice && (
          <View style={styles.metaItem}>
            <Text style={styles.metaLabel}>{t('notifications.sourceDevice') || 'Source'}</Text>
            <Text style={styles.metaValue}>{item.sourceDevice}</Text>
          </View>
        )}

        {/* Timestamp */}
        <View style={styles.metaItem}>
          <Text style={styles.metaLabel}>{t('notifications.timestamp') || 'Time'}</Text>
          <Text style={styles.metaValue}>{new Date(item.createdAt).toLocaleString()}</Text>
        </View>
      </ScrollView>
    </View>
  );
}
