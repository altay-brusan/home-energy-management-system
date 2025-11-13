import React, { JSX, useMemo } from 'react';
import { View, Text, Pressable, ScrollView } from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { ArrowLeft } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeNotificationDetailStyles } from './NotificationDetailScreen.styles';
import { Card } from '../../../components';
import Button from '../../../components/ui/Button';
import { useI18n } from '../../../hooks/useI18n';

import { DeviceNotification } from '../../../hooks/useDeviceNotifications';

type DetailItem = DeviceNotification;

type ParamList = {
  NotificationDetail: { item: DetailItem };
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

  const criticalFg = (COLORS as any).critical ?? '#EB5757';
  const criticalBg =
    (COLORS as any).criticalBg ??
    (isDark ? 'rgba(235,87,87,0.18)' : 'rgba(235,87,87,0.12)');

  return (
    <View style={[styles.screen, { paddingTop: insets.top + 8 }]}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <Pressable
            onPress={() => navigation.goBack()}
            style={styles.backButton}
          >
            <ArrowLeft size={26} color={styles.headerTitle.color} strokeWidth={2.5} />
          </Pressable>
          <Text style={styles.headerTitle}>{t('notifications.details')}</Text>
        </View>
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Title and Badge */}
        <View style={styles.titleContainer}>
          <Text style={styles.title}>{item.title}</Text>
          {item.severity === 'critical' && (
            <View style={[styles.criticalBadge, { backgroundColor: criticalBg }]}>
              <Text style={[styles.criticalBadgeText, { color: criticalFg }]}>
                {t('notifications.critical')}
              </Text>
            </View>
          )}
        </View>

        {/* Message */}
        <Text style={styles.message}>{item.message}</Text>

        {/* Additional Information */}
        {item.description && (
          <View style={styles.infoCard}>
            <View style={styles.infoHeader}>
              <View style={styles.infoIcon}>
                <FontAwesome name="info-circle" size={18} color={COLORS.accent || '#1DD1A1'} />
              </View>
              <Text style={styles.infoTitle}>{t('notifications.additionalInfo')}</Text>
            </View>
            <Text style={styles.infoText}>{item.description}</Text>
          </View>
        )}

        {/* Timestamp - Bottom Center */}
        <Text style={styles.timestamp}>
          {new Date(item.createdAt).toLocaleString()}
        </Text>
      </ScrollView>
    </View>
  );
}
