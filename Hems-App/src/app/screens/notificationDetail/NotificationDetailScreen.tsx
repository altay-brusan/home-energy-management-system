import React, { JSX, useMemo } from 'react';
import { View, Text } from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeNotificationDetailStyles } from './NotificationDetailScreen.styles';
import { Card } from '../../../components';
import Button from '../../../components/ui/Button';

type DetailItem = {
  id: string;
  title: string;
  message: string;
  description: string;
  createdAt: string;
  read: boolean;
  severity: 'critical' | 'normal';
};

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

  const criticalFg = (COLORS as any).critical ?? '#EB5757';
  const criticalBg =
    (COLORS as any).criticalBg ??
    (isDark ? 'rgba(235,87,87,0.18)' : 'rgba(235,87,87,0.12)');

  return (
    <MainLayout>
      <View style={styles.container}>
        <View style={styles.pageTitleRow}>
          <Text style={styles.pageTitle}>{item.title}</Text>
          {item.severity === 'critical' && (
            <View style={[styles.badge, { backgroundColor: criticalBg }]}>
              <Text style={[styles.badgeText, { color: criticalFg }]}>
                CRITICAL
              </Text>
            </View>
          )}
        </View>

        <Card padding={20} radius={18} style={styles.card}>
          <Text style={styles.itemTitle}>{item.message}</Text>

          <View style={styles.divider} />

          {item.description && (
            <Text style={styles.description}>{item.description}</Text>
          )}

          <View style={styles.footerRow}>
            <Text style={styles.time}>
              {new Date(item.createdAt).toLocaleString()}
            </Text>
          </View>
        </Card>

        <Button
          title="Back"
          onPress={() => navigation.goBack()}
          style={styles.button}
          textStyle={styles.buttonText}
        />
      </View>
    </MainLayout>
  );
}
