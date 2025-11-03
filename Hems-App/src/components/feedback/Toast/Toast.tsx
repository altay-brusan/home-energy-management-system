import React, { useEffect, useRef } from 'react';
import { Animated, Pressable, Text, View, Dimensions, Platform } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { createToastStyles } from './Toast.styles';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastProps {
    visible: boolean;
    message: string;
    type?: ToastType;
    title?: string;
    duration?: number;
    onHide: () => void;
}

export default function Toast ({
    visible,
    message,
    type = 'info',
    title,
    duration = 4000,
    onHide
}: ToastProps) {
    const insets = useSafeAreaInsets();
    const mode = useThemeStore((s: any) => s.mode);
    const isDark = mode === 'dark';
    const COLORS = getThemeColors(isDark);
    const styles = createToastStyles(COLORS, isDark);
    const { width: screenWidth } = Dimensions.get('window');
    const slideAnim = useRef(new Animated.Value(-120)).current;
    const opacityAnim = useRef(new Animated.Value(0)).current;
    const scaleAnim = useRef(new Animated.Value(0.8)).current;
    const progressAnim = useRef(new Animated.Value(0)).current;
    const pulseAnim = useRef(new Animated.Value(1)).current;

    const getToastConfig = () => {
        switch (type) {
            case 'success':
                return {
                    icon: 'check-circle' as const,
                    color: '#10B981',
                    backgroundColor: isDark ? 'rgba(16, 185, 129, 0.2)' : 'rgba(16, 185, 129, 0.15)',
                    borderColor: '#10B981',
                    title: title || 'Başarılı',
                    gradient: ['#10B981', '#059669'],
                };
            case 'error':
                return {
                    icon: 'exclamation-circle' as const,
                    color: '#EF4444',
                    backgroundColor: isDark ? 'rgba(239, 68, 68, 0.2)' : 'rgba(239, 68, 68, 0.15)',
                    borderColor: '#EF4444',
                    title: title || 'Hata',
                    gradient: ['#EF4444', '#DC2626'],
                };
            case 'warning':
                return {
                    icon: 'exclamation-triangle' as const,
                    color: '#F59E0B',
                    backgroundColor: isDark ? 'rgba(245, 158, 11, 0.2)' : 'rgba(245, 158, 11, 0.15)',
                    borderColor: '#F59E0B',
                    title: title || 'Uyarı',
                    gradient: ['#F59E0B', '#D97706'],
                };
            case 'info':
            default:
                return {
                    icon: 'info-circle' as const,
                    color: '#3B82F6',
                    backgroundColor: isDark ? 'rgba(59, 130, 246, 0.2)' : 'rgba(59, 130, 246, 0.15)',
                    borderColor: '#3B82F6',
                    title: title || 'Bilgi',
                    gradient: ['#3B82F6', '#2563EB'],
                };
        }
    };

    const config = getToastConfig();

    useEffect(() => {
        if (visible) {
            // Show animation with bounce effect
            Animated.parallel([
                Animated.spring(slideAnim, {
                    toValue: 0,
                    tension: 100,
                    friction: 8,
                    useNativeDriver: true,
                }),
                Animated.timing(opacityAnim, {
                    toValue: 1,
                    duration: 300,
                    useNativeDriver: true,
                }),
                Animated.spring(scaleAnim, {
                    toValue: 1,
                    tension: 100,
                    friction: 8,
                    useNativeDriver: true,
                }),
            ]).start();

            // Haptic feedback removed due to compatibility issues

            Animated.timing(progressAnim, {
                toValue: 1,
                duration: duration,
                useNativeDriver: false,
            }).start();

            const pulseAnimation = Animated.loop(
                Animated.sequence([
                    Animated.timing(pulseAnim, {
                        toValue: 1.1,
                        duration: 1000,
                        useNativeDriver: true,
                    }),
                    Animated.timing(pulseAnim, {
                        toValue: 1,
                        duration: 1000,
                        useNativeDriver: true,
                    }),
                ])
            );
            pulseAnimation.start();

            const timer = setTimeout(() => {
                hideToast();
            }, duration);

            return () => clearTimeout(timer);
        } else {
            hideToast();
        }
    }, [visible, duration]);

    const hideToast = () => {
        Animated.parallel([
            Animated.timing(slideAnim, {
                toValue: -120,
                duration: 250,
                useNativeDriver: true,
            }),
            Animated.timing(opacityAnim, {
                toValue: 0,
                duration: 250,
                useNativeDriver: true,
            }),
            Animated.timing(scaleAnim, {
                toValue: 0.8,
                duration: 250,
                useNativeDriver: true,
            }),
        ]).start(() => {
            progressAnim.setValue(0);
            onHide();
        });
    };

    if (!visible) return null;

    return (
        <Animated.View
            style={[
                styles.toastContainer,
                {
                    transform: [
                        { translateY: slideAnim },
                        { scale: scaleAnim }
                    ],
                    opacity: opacityAnim,
                }
            ]}
        >
            <Pressable onPress={hideToast}>
                <Animated.View
                    style={[
                        styles.toast,
                        styles[type],
                        {
                            borderLeftColor: config.color,
                            borderTopColor: config.color,
                            borderRightColor: config.color,
                            borderBottomColor: config.color,
                            ...Platform.select({
                                ios: {
                                    shadowColor: config.color,
                                },
                            }),
                        }
                    ]}
                >
                    <Animated.View
                        style={[
                            styles.iconContainer,
                            (styles as any)[`${type}Icon`],
                            {
                                backgroundColor: config.backgroundColor,
                                transform: [{ scale: pulseAnim }],
                            }
                        ]}
                    >
                        <FontAwesome
                            name={config.icon}
                            size={18}
                            color={config.color}
                        />
                    </Animated.View>
                    <View style={styles.contentContainer}>
                        <Text
                            style={[
                                styles.title,
                                { color: config.color }
                            ]}
                        >
                            {config.title}
                        </Text>
                        <Text style={styles.message}>
                            {message}
                        </Text>
                    </View>
                    <Pressable
                        onPress={hideToast}
                        style={{
                            marginLeft: SPACING.sm,
                            padding: 8,
                            borderRadius: 12,
                            backgroundColor: isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.05)',
                        }}
                    >
                        <FontAwesome
                            name="times"
                            size={14}
                            color={isDark ? COLORS.textSoft : COLORS.slate900}
                        />
                    </Pressable>
                </Animated.View>

                <View style={styles.progressBarContainer}>
                    <Animated.View
                        style={[
                            styles.progressBar,
                            (styles as any)[`${type}Progress`],
                            {
                                backgroundColor: config.color,
                                width: progressAnim.interpolate({
                                    inputRange: [0, 1],
                                    outputRange: ['0%', '100%'],
                                }),
                            }
                        ]}
                    />
                </View>
            </Pressable>
        </Animated.View>
    );
}
