import FontAwesome from '@react-native-vector-icons/fontawesome';
import React, { useEffect, useRef } from 'react';
import { Animated, Easing, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeOfflineScreenStyles } from './OfflineScreen.styles';
import { useI18n } from '../../../hooks/useI18n';


interface OfflineScreenProps {
    onRetry?: () => void;
}

export default function OfflineScreen ({ onRetry }: OfflineScreenProps) {
    const isDark = useThemeStore(s => s.mode) === 'dark';
    const COLORS = getThemeColors(isDark);
    const styles = makeOfflineScreenStyles(COLORS);
    const insets = useSafeAreaInsets();
    const { t } = useI18n();

    const fadeAnim = useRef(new Animated.Value(0)).current;
    const slideAnim = useRef(new Animated.Value(30)).current;
    const pulseAnim = useRef(new Animated.Value(1)).current;

    useEffect(() => {
        Animated.parallel([
            Animated.timing(fadeAnim, {
                toValue: 1,
                duration: 600,
                useNativeDriver: true,
            }),
            Animated.timing(slideAnim, {
                toValue: 0,
                duration: 600,
                easing: Easing.out(Easing.cubic),
                useNativeDriver: true,
            }),
        ]).start();

        const pulseAnimation = Animated.loop(
            Animated.sequence([
                Animated.timing(pulseAnim, {
                    toValue: 1.05,
                    duration: 2000,
                    useNativeDriver: true,
                }),
                Animated.timing(pulseAnim, {
                    toValue: 1,
                    duration: 2000,
                    useNativeDriver: true,
                }),
            ])
        );
        pulseAnimation.start();

        return () => {
            pulseAnimation.stop();
        };
    }, []);

    return (
        <View style={[styles.container, { paddingTop: insets.top }]}>
            <ScrollView
                contentContainerStyle={styles.scrollContent}
                showsVerticalScrollIndicator={false}
                bounces={false}
            >
                <Animated.View
                    style={[
                        styles.content,
                        {
                            opacity: fadeAnim,
                            transform: [{ translateY: slideAnim }],
                        }
                    ]}
                >
                    <View style={styles.iconSection}>
                        <Animated.View
                            style={[
                                styles.iconContainer,
                                { transform: [{ scale: pulseAnim }] }
                            ]}
                        >
                            <FontAwesome
                                name="wifi"
                                size={48}
                                color={COLORS.accent}
                            />
                            <View style={styles.slashOverlay}>
                                <FontAwesome
                                    name="times"
                                    size={20}
                                    color={COLORS.error}
                                />
                            </View>
                        </Animated.View>
                    </View>

                    <View style={styles.messageSection}>
                        <Text style={styles.title}>{t('offline.title')}</Text>
                        <Text style={styles.subtitle}>
                            {t('offline.subtitle')}
                        </Text>
                    </View>

                    <View style={styles.tipsSection}>
                        <View style={styles.tip}>
                            <FontAwesome name="wifi" size={14} color={COLORS.accent} />
                            <Text style={styles.tipText}>{t('offline.tips.checkWifi')}</Text>
                        </View>
                        <View style={styles.tip}>
                            <FontAwesome name="signal" size={14} color={COLORS.accent} />
                            <Text style={styles.tipText}>{t('offline.tips.moveToSignal')}</Text>
                        </View>
                    </View>

                    <View style={styles.actionsSection}>
                        <TouchableOpacity
                            style={styles.retryButton}
                            onPress={onRetry}
                            activeOpacity={0.7}
                        >
                            <FontAwesome
                                name="refresh"
                                size={16}
                                color={COLORS.white}
                                style={styles.buttonIcon}
                            />
                            <Text style={styles.retryText}>{t('offline.tryAgain')}</Text>
                        </TouchableOpacity>

                        <TouchableOpacity
                            style={styles.settingsButton}
                            onPress={() => { }}
                            activeOpacity={0.7}
                        >
                            <FontAwesome
                                name="cog"
                                size={16}
                                color={COLORS.textSoft}
                                style={styles.buttonIcon}
                            />
                            <Text style={styles.settingsText}>{t('offline.networkSettings')}</Text>
                        </TouchableOpacity>
                    </View>

                    <View style={styles.statusSection}>
                        <View style={styles.statusDot} />
                        <Text style={styles.statusText}>{t('offline.checkingConnection')}</Text>
                    </View>
                </Animated.View>
            </ScrollView>
        </View>
    );
}
