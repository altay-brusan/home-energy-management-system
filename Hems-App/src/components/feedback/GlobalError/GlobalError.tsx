import React, { useEffect, useRef } from 'react';
import { Animated, Pressable, Text, View } from 'react-native';
import { useErrorStore } from '../../../store/errorStore';
import { getGlobalErrorStyles } from './GlobalError.styles';

export default function GlobalError() {
  const err = useErrorStore((s) => s.current);
  const clear = useErrorStore((s) => s.clear);
  const anim = useRef(new Animated.Value(0)).current;
  const styles = getGlobalErrorStyles();

  useEffect(() => {
    if (err) {
      Animated.timing(anim, { toValue: 1, duration: 180, useNativeDriver: true }).start();
    } else {
      Animated.timing(anim, { toValue: 0, duration: 150, useNativeDriver: true }).start();
    }
  }, [err, anim]);

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
      <Pressable onPress={clear} style={styles.card}>
        <Text style={styles.title}>Hata</Text>
        <Text style={styles.msg}>{err.message}</Text>
        {!!err.status && <Text style={styles.meta}>Kod: {err.status} • {err.code}</Text>}
      </Pressable>
    </Animated.View>
  );
}
