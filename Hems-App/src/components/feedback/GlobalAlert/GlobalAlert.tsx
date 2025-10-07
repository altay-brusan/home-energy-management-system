import React, { useEffect, useRef } from 'react';
import { Animated, Pressable, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAlertStore } from '../../../store/alertStore';
import { getGlobalAlertStyles } from './GlobalAlert.styles';
import { SPACING, getThemeColors } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import Button from '../../ui/Button';

export default function GlobalAlert() {
  const visible = useAlertStore(s => s.visible);
  const title = useAlertStore(s => s.title);
  const message = useAlertStore(s => s.message);
  const hide = useAlertStore(s => s.hide);
  const level = useAlertStore(s => s.level);
  const deviceName = useAlertStore(s => s.deviceName);
  const recommendation = useAlertStore(s => s.recommendation);
  const timestamp = useAlertStore(s => s.timestamp);
  const onDetails = useAlertStore(s => s.onDetails);
  const onSupport = useAlertStore(s => s.onSupport);
  const show = useAlertStore(s => s.show);
  const insets = useSafeAreaInsets();

  const styles = getGlobalAlertStyles();
  const anim = useRef(new Animated.Value(0)).current;
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);

  const levelColor =
    level === 'critical'
      ? COLORS.error
      : level === 'warning'
      ? COLORS.warning
      : COLORS.info;
  const iconName =
    level === 'critical'
      ? 'exclamation-triangle'
      : level === 'warning'
      ? 'exclamation-circle'
      : 'info-circle';
  const levelLabel =
    level === 'critical' ? 'Kritik' : level === 'warning' ? 'Önemli' : 'Bilgi';

  const formattedTime = (() => {
    if (!timestamp) return '';
    const d = new Date(timestamp);
    const pad = (n: number) => (n < 10 ? `0${n}` : `${n}`);
    return `${pad(d.getDate())}.${pad(
      d.getMonth() + 1,
    )}.${d.getFullYear()} • ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  })();

  useEffect(() => {
    const id = setInterval(() => {
      show({
        title: 'Kritik Uyarı',
        message:
          'A cihazınız olması gerekenden fazla ısındı, lütfen kontrol edin.',
        level: 'critical',
      });
    }, 5 * 60 * 1000); // Çalışma için 30 saniyedir sunum sonrası  bu kural kaldıralacak.
    return () => clearInterval(id);
  }, [show]);

  useEffect(() => {
    Animated.timing(anim, {
      toValue: visible ? 1 : 0,
      duration: 180,
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
            paddingBottom: insets.bottom + SPACING.md,
            transform: [
              {
                translateY: anim.interpolate({
                  inputRange: [0, 1],
                  outputRange: [40, 0],
                }),
              },
            ],
          },
        ]}
      >
        <View style={[styles.accentBar, { backgroundColor: levelColor }]} />
        <View style={styles.handle} />
        <View style={styles.titleRow}>
          <View style={[styles.iconWrap, { borderColor: levelColor }]}>
            <FontAwesome name={iconName as any} size={18} color={levelColor} />
          </View>
          <Text
            style={[styles.title, { color: levelColor, flex: 1 }]}
            numberOfLines={2}
          >
            {title}
          </Text>
          <View style={[styles.levelChip, { borderColor: levelColor }]}>
            <Text style={[styles.levelChipText, { color: levelColor }]}>
              {levelLabel}
            </Text>
          </View>
        </View>
        <Text style={styles.msg}>
          {message}
          {deviceName ? ` (Cihaz: ${deviceName})` : ''}
        </Text>
        {formattedTime ? (
          <Text style={styles.meta}>{formattedTime}</Text>
        ) : null}
        <Text style={styles.sectionTitle}>Öneri</Text>
        <Text style={styles.msg}>
          {recommendation ||
            'Lütfen cihazı kontrol edin ve gerekli önlemleri alınız. Sorun devam ederse destek ile iletişime geçin.'}
        </Text>
        <View style={styles.btnRow}>
          <Button
            title="Detaya Git"
            onPress={() => {
              hide();
              if (onDetails) onDetails();
            }}
            variant="primary"
            style={[styles.btnHalf]}
          />
          <Button
            title="Destek"
            onPress={() => {
              hide();
              if (onSupport) onSupport();
            }}
            variant="secondary"
            style={styles.btnHalf}
          />
          <Button
            title="Kapat"
            onPress={hide}
            variant="primary"
            style={[styles.closeBtn, { backgroundColor: levelColor }]}
            textStyle={{ color: COLORS.white, fontWeight: '700' }}
          />
        </View>
      </Animated.View>
    </Animated.View>
  );
}
