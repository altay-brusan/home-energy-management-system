import React, { useEffect, useRef } from 'react';
import { Animated, Pressable, Text, View, StyleSheet, Platform } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING, TYPOGRAPHY } from '../../../theme/constants';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastProps {
  visible: boolean;
  message: string;
  type?: ToastType;
  title?: string;
  duration?: number;
  onHide: () => void;
}

const getToastConfig = (type: ToastType, isDark: boolean) => {
  const configs = {
    success: {
      icon: CheckCircle,
      color: '#10B981',
      bgColor: isDark ? 'rgba(16, 185, 129, 0.15)' : 'rgba(16, 185, 129, 0.1)',
      borderColor: isDark ? 'rgba(16, 185, 129, 0.3)' : 'rgba(16, 185, 129, 0.2)',
      iconBg: isDark ? 'rgba(16, 185, 129, 0.2)' : 'rgba(16, 185, 129, 0.15)',
    },
    error: {
      icon: XCircle,
      color: '#EF4444',
      bgColor: isDark ? 'rgba(239, 68, 68, 0.15)' : 'rgba(239, 68, 68, 0.1)',
      borderColor: isDark ? 'rgba(239, 68, 68, 0.3)' : 'rgba(239, 68, 68, 0.2)',
      iconBg: isDark ? 'rgba(239, 68, 68, 0.2)' : 'rgba(239, 68, 68, 0.15)',
    },
    warning: {
      icon: AlertTriangle,
      color: '#F59E0B',
      bgColor: isDark ? 'rgba(245, 158, 11, 0.15)' : 'rgba(245, 158, 11, 0.1)',
      borderColor: isDark ? 'rgba(245, 158, 11, 0.3)' : 'rgba(245, 158, 11, 0.2)',
      iconBg: isDark ? 'rgba(245, 158, 11, 0.2)' : 'rgba(245, 158, 11, 0.15)',
    },
    info: {
      icon: Info,
      color: '#3B82F6',
      bgColor: isDark ? 'rgba(59, 130, 246, 0.15)' : 'rgba(59, 130, 246, 0.1)',
      borderColor: isDark ? 'rgba(59, 130, 246, 0.3)' : 'rgba(59, 130, 246, 0.2)',
      iconBg: isDark ? 'rgba(59, 130, 246, 0.2)' : 'rgba(59, 130, 246, 0.15)',
    },
  };
  return configs[type];
};

export default function Toast({
  visible,
  message,
  type = 'info',
  title,
  duration = 4000,
  onHide,
}: ToastProps) {
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s: any) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const translateY = useRef(new Animated.Value(-100)).current;
  const opacity = useRef(new Animated.Value(0)).current;
  const scale = useRef(new Animated.Value(0.9)).current;

  const config = getToastConfig(type, isDark);
  const IconComponent = config.icon;

  useEffect(() => {
    if (visible) {
      // Animate in
      Animated.parallel([
        Animated.spring(translateY, {
          toValue: 0,
          tension: 80,
          friction: 10,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: 200,
          useNativeDriver: true,
        }),
        Animated.spring(scale, {
          toValue: 1,
          tension: 80,
          friction: 10,
          useNativeDriver: true,
        }),
      ]).start();

      // Auto hide
      const timer = setTimeout(hideToast, duration);
      return () => clearTimeout(timer);
    }
  }, [visible, duration]);

  const hideToast = () => {
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: -100,
        duration: 200,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
      Animated.timing(scale, {
        toValue: 0.9,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start(() => onHide());
  };

  if (!visible) return null;

  return (
    <Animated.View
      style={[
        styles.container,
        {
          top: insets.top + 12,
          transform: [{ translateY }, { scale }],
          opacity,
        },
      ]}
    >
      <Pressable onPress={hideToast} style={{ width: '100%' }}>
        <View
          style={[
            styles.toast,
            {
              backgroundColor: isDark ? COLORS.surface : COLORS.white,
              borderColor: config.borderColor,
              ...Platform.select({
                ios: {
                  shadowColor: isDark ? '#000' : config.color,
                  shadowOffset: { width: 0, height: 4 },
                  shadowOpacity: isDark ? 0.3 : 0.15,
                  shadowRadius: 12,
                },
                android: {
                  elevation: 8,
                },
              }),
            },
          ]}
        >
          {/* Left accent bar */}
          <View
            style={[
              styles.accentBar,
              { backgroundColor: config.color },
            ]}
          />

          {/* Icon */}
          <View
            style={[
              styles.iconContainer,
              { backgroundColor: config.iconBg },
            ]}
          >
            <IconComponent size={20} color={config.color} strokeWidth={2.5} />
          </View>

          {/* Content */}
          <View style={styles.content}>
            {title && (
              <Text
                style={[
                  styles.title,
                  { color: isDark ? COLORS.text : COLORS.slate900 },
                ]}
                numberOfLines={1}
              >
                {title}
              </Text>
            )}
            <Text
              style={[
                styles.message,
                { color: COLORS.textSoft },
                !title && { marginTop: 0 },
              ]}
              numberOfLines={2}
            >
              {message}
            </Text>
          </View>

          {/* Close button */}
          <Pressable
            onPress={hideToast}
            style={[
              styles.closeButton,
              { backgroundColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.05)' },
            ]}
            hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
          >
            <X size={14} color={COLORS.textSoft} strokeWidth={2.5} />
          </Pressable>
        </View>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    left: 0,
    right: 0,
    zIndex: 10000,
    alignItems: 'center',
    paddingHorizontal: 16,
  },
  toast: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingLeft: 0,
    paddingRight: 12,
    borderRadius: 14,
    borderWidth: 1,
    overflow: 'hidden',
  },
  accentBar: {
    width: 4,
    height: '100%',
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    borderTopLeftRadius: 14,
    borderBottomLeftRadius: 14,
  },
  iconContainer: {
    width: 36,
    height: 36,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    marginLeft: 14,
  },
  content: {
    flex: 1,
    marginLeft: 12,
    marginRight: 8,
  },
  title: {
    fontSize: 15,
    fontWeight: '600',
    fontFamily: 'Montserrat-SemiBold',
    marginBottom: 2,
  },
  message: {
    fontSize: 13,
    fontFamily: 'Montserrat-Regular',
    lineHeight: 18,
  },
  closeButton: {
    width: 28,
    height: 28,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
});
