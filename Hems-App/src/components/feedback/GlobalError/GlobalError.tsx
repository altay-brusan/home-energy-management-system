import React, { useEffect, useRef } from 'react';
import { Animated, Pressable, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useErrorStore } from '../../../store/errorStore';
import { getGlobalErrorStyles } from './GlobalError.styles';

export default function GlobalError () {
  const { t } = useTranslation();
  const err = useErrorStore((s) => s.current);
  const clear = useErrorStore((s) => s.clear);
  const anim = useRef(new Animated.Value(0)).current;
  const styles = getGlobalErrorStyles();

  useEffect(() => {
    if (err) {
      Animated.timing(anim, { toValue: 1, duration: 180, useNativeDriver: true }).start();

      // Otomatik kapanma - 5 saniye sonra
      const timer = setTimeout(() => {
        clear();
      }, 5000);

      return () => clearTimeout(timer);
    } else {
      Animated.timing(anim, { toValue: 0, duration: 150, useNativeDriver: true }).start();
    }
  }, [err, anim, clear]);

  const handleDismiss = () => {
    clear();
  };

  if (!err) return null;

  return (
    <Animated.View
      style={[
        styles.wrap,
        {
          opacity: anim,
          transform: [{ translateY: anim.interpolate({ inputRange: [0, 1], outputRange: [-30, 0] }) }],
        },
      ]}
    >
      <Pressable onPress={handleDismiss} style={styles.card}>
        <Text style={styles.title}>{t('globalError.title')}</Text>
        <Text style={styles.msg}>{err.message}</Text>
        {!!err.status && <Text style={styles.meta}>{t('globalError.code')}: {err.status} • {err.code}</Text>}
        <Text style={styles.retryText}>{t('globalError.close')}</Text>
      </Pressable>
    </Animated.View>
  );
}
