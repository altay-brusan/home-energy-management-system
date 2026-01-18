import React, { useEffect, useRef } from 'react';
import {
    Animated,
    Easing,
    Linking,
    ScrollView,
    Text,
    TouchableOpacity,
    View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Download, AlertTriangle, ArrowUpCircle, Clock } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeUpdateRequiredStyles } from './UpdateRequiredScreen.styles';
import { useI18n } from '../../../hooks/useI18n';
import { getAppVersion } from '../../../api/services/versionService';

interface UpdateRequiredScreenProps {
    latestVersion: string;
    storeUrl: string | null;
    description: string | null;
    revocationReason: string | null;
    isRequired: boolean;
    onSkip?: () => void;
}

export default function UpdateRequiredScreen({
    latestVersion,
    storeUrl,
    description,
    revocationReason,
    isRequired,
    onSkip,
}: UpdateRequiredScreenProps) {
    const isDark = useThemeStore(s => s.mode) === 'dark';
    const COLORS = getThemeColors(isDark);
    const styles = makeUpdateRequiredStyles(COLORS);
    const insets = useSafeAreaInsets();
    const { t } = useI18n();
    const currentVersion = getAppVersion();

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
    }, [fadeAnim, slideAnim, pulseAnim]);

    const handleUpdate = async () => {
        if (storeUrl) {
            try {
                const canOpen = await Linking.canOpenURL(storeUrl);
                if (canOpen) {
                    await Linking.openURL(storeUrl);
                }
            } catch (error) {
                console.warn('[UpdateRequiredScreen] Failed to open store URL:', error);
            }
        }
    };

    const handleSkip = () => {
        if (!isRequired && onSkip) {
            onSkip();
        }
    };

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
                            <ArrowUpCircle
                                size={48}
                                color={COLORS.accent}
                                strokeWidth={1.5}
                            />
                            <View style={styles.badgeOverlay}>
                                <Download
                                    size={16}
                                    color={COLORS.white}
                                    strokeWidth={2.5}
                                />
                            </View>
                        </Animated.View>
                    </View>

                    <View style={styles.messageSection}>
                        <Text style={styles.title}>
                            {isRequired
                                ? t('update.requiredTitle', 'Update Required')
                                : t('update.availableTitle', 'Update Available')}
                        </Text>
                        <Text style={styles.subtitle}>
                            {isRequired
                                ? t('update.requiredSubtitle', 'A new version of the app is required to continue. Please update to the latest version.')
                                : t('update.availableSubtitle', 'A new version of the app is available. Update now for the best experience.')}
                        </Text>
                    </View>

                    {revocationReason && (
                        <View style={styles.revocationSection}>
                            <View style={styles.revocationCard}>
                                <AlertTriangle
                                    size={18}
                                    color={COLORS.error}
                                    style={styles.revocationIcon}
                                />
                                <Text style={styles.revocationText}>
                                    {revocationReason}
                                </Text>
                            </View>
                        </View>
                    )}

                    <View style={styles.versionSection}>
                        <View style={styles.versionCard}>
                            <Text style={styles.versionLabel}>
                                {t('update.currentVersion', 'Current Version')}
                            </Text>
                            <Text style={styles.versionValue}>{currentVersion}</Text>
                        </View>
                        <View style={styles.versionCard}>
                            <Text style={styles.versionLabel}>
                                {t('update.latestVersion', 'Latest Version')}
                            </Text>
                            <Text style={styles.newVersionValue}>{latestVersion}</Text>
                        </View>
                    </View>

                    {description && (
                        <View style={styles.descriptionSection}>
                            <View style={styles.descriptionCard}>
                                <Text style={styles.descriptionTitle}>
                                    {t('update.whatsNew', "What's New")}
                                </Text>
                                <Text style={styles.descriptionText}>
                                    {description}
                                </Text>
                            </View>
                        </View>
                    )}

                    <View style={styles.actionsSection}>
                        <TouchableOpacity
                            style={styles.updateButton}
                            onPress={handleUpdate}
                            activeOpacity={0.7}
                            disabled={!storeUrl}
                        >
                            <Download
                                size={18}
                                color={COLORS.white}
                                style={styles.buttonIcon}
                            />
                            <Text style={styles.updateButtonText}>
                                {t('update.updateNow', 'Update Now')}
                            </Text>
                        </TouchableOpacity>

                        {!isRequired && onSkip && (
                            <TouchableOpacity
                                style={styles.laterButton}
                                onPress={handleSkip}
                                activeOpacity={0.7}
                            >
                                <Clock
                                    size={16}
                                    color={COLORS.textSoft}
                                    style={styles.buttonIcon}
                                />
                                <Text style={styles.laterButtonText}>
                                    {t('update.later', 'Remind Me Later')}
                                </Text>
                            </TouchableOpacity>
                        )}
                    </View>

                    {isRequired && (
                        <Text style={styles.footerText}>
                            {t('update.requiredFooter', 'This update is required to continue using the app.')}
                        </Text>
                    )}
                </Animated.View>
            </ScrollView>
        </View>
    );
}
