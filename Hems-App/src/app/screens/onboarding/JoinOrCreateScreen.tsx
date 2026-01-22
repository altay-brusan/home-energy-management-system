import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useMemo, useState, useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Keyboard,
  KeyboardAvoidingView,
  Text,
  TouchableOpacity,
  TouchableWithoutFeedback,
  View,
  ActivityIndicator,
} from 'react-native';
import { LogOut, Home, Users, Mail } from 'lucide-react-native';

import { Card, ThemeToggle, LanguageSelector } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import { usePlatform } from '../../../hooks/usePlatform';
import useAuth from '../../../hooks/useAuth';
import { useSitesStore } from '../../../store/sitesStore';
import { useThemeStore } from '../../../store/themeStore';
import { useToastStore } from '../../../store/toastStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';
import { getOnboardingStyles } from './Onboarding.styles';
import { invitationsService } from '../../../api/services/devicesharingService';
import { PendingInvitationDto } from '../../../api/types/invitations.types';
import { navigationRef } from '../../routes';

type RootStackParamList = {
  Home: undefined;
  HomeSetup: undefined;
  MainTabs: undefined;
  JoinOrCreate: undefined;
};

export default function JoinOrCreateScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { getUserSites, setSelectedSite } = useSitesStore();
  const { isIOS } = usePlatform();
  const { logout } = useAuth();
  const { show: showToast } = useToastStore();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getOnboardingStyles(COLORS, isDark), [COLORS, isDark]);

  const [isLoading, setIsLoading] = useState(true);
  const [pendingInvitations, setPendingInvitations] = useState<PendingInvitationDto[]>([]);
  const [showCodeEntry, setShowCodeEntry] = useState(false);
  const [invitationCode, setInvitationCode] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  // Check for pending invitations on mount
  useEffect(() => {
    const checkPendingInvitations = async () => {
      try {
        setIsLoading(true);
        const result = await invitationsService.getPendingInvitations();
        if (result.hasPendingInvitations) {
          setPendingInvitations(result.invitations);
        }
      } catch (err) {
        console.log('[JoinOrCreate] Error checking pending invitations:', err);
      } finally {
        setIsLoading(false);
      }
    };

    checkPendingInvitations();
  }, []);

  const handleCreateHome = useCallback(() => {
    navigation.navigate('HomeSetup');
  }, [navigation]);

  const handleJoinSharedHome = useCallback(() => {
    setShowCodeEntry(true);
    setError('');
  }, []);

  const handleBack = useCallback(() => {
    setShowCodeEntry(false);
    setInvitationCode('');
    setError('');
  }, []);

  const handleAcceptInvitation = useCallback(async (token?: string) => {
    const codeToUse = token || invitationCode.trim();

    if (!codeToUse) {
      setError(t('joinOrCreate.enterCode'));
      return;
    }

    // Validate 6-digit code
    if (!/^\d{6}$/.test(codeToUse)) {
      setError(t('joinOrCreate.invalidCodeFormat'));
      return;
    }

    setIsSubmitting(true);
    setError('');

    try {
      await invitationsService.acceptInvitation({ token: codeToUse });

      // Refresh sites to get the newly shared site
      const updatedDashboard = await getUserSites();
      const updatedSites = updatedDashboard?.siteList || [];

      if (updatedSites.length > 0) {
        // Select the first site (most likely the newly added one)
        setSelectedSite(updatedSites[0]);

        showToast({
          type: 'success',
          title: t('invitation.accepted'),
          message: t('invitation.homeListUpdated'),
          duration: 3000,
        });

        // Navigate to main app
        navigationRef.current?.reset({
          index: 0,
          routes: [{ name: 'MainTabs' }],
        });
      } else {
        setError(t('joinOrCreate.noSiteReceived'));
      }
    } catch (err: any) {
      const errorMessage = err?.response?.data?.message || err?.message || t('invitation.acceptFailed');
      setError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  }, [invitationCode, getUserSites, setSelectedSite, showToast, t]);

  const handleLogout = useCallback(() => {
    Alert.alert(
      t('profile.logoutConfirm.title', 'Logout'),
      t('profile.logoutConfirm.message', 'Are you sure you want to logout?'),
      [
        { text: t('common.cancel', 'Cancel'), style: 'cancel' },
        { text: t('profile.logout', 'Logout'), style: 'destructive', onPress: logout },
      ]
    );
  }, [logout, t]);

  // Show loading state while checking for invitations
  if (isLoading) {
    return (
      <MainLayout>
        <View style={[styles.root, { justifyContent: 'center', alignItems: 'center' }]}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={{ color: COLORS.textSoft, marginTop: 16 }}>
            {t('common.loading')}
          </Text>
        </View>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <View style={styles.root}>
          <View style={[styles.topBar, { justifyContent: 'space-between', paddingHorizontal: 16 }]}>
            <TouchableOpacity onPress={handleLogout} style={{ padding: 8 }}>
              <LogOut size={22} color={COLORS.textSoft} />
            </TouchableOpacity>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
              <LanguageSelector />
              <ThemeToggle style={styles.themeToggle} />
            </View>
          </View>

          <View style={styles.content}>
            <View style={styles.bgTopShade} pointerEvents="none" />
            <View style={styles.bgBottomShade} pointerEvents="none" />

            <KeyboardAvoidingView
              style={styles.keyboardAvoider}
              behavior={isIOS ? 'padding' : 'height'}
              keyboardVerticalOffset={isIOS ? 0 : 0}
            >
              <View style={styles.scrollContent}>
                <Card variant="elevated" padding={24} style={styles.card}>
                  <View style={styles.inner}>
                    {!showCodeEntry ? (
                      <>
                        <Text style={styles.title}>{t('joinOrCreate.title')}</Text>
                        <Text style={styles.subtitle}>{t('joinOrCreate.subtitle')}</Text>

                        <View style={styles.formWrapper}>
                          {/* Show pending invitations if any */}
                          {pendingInvitations.length > 0 && (
                            <View style={{
                              backgroundColor: isDark ? 'rgba(59, 130, 246, 0.15)' : 'rgba(59, 130, 246, 0.1)',
                              borderRadius: 16,
                              padding: 16,
                              marginBottom: 16,
                              borderWidth: 1,
                              borderColor: isDark ? 'rgba(59, 130, 246, 0.3)' : 'rgba(59, 130, 246, 0.2)',
                            }}>
                              <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 12 }}>
                                <Mail size={20} color="#3b82f6" />
                                <Text style={{
                                  marginLeft: 8,
                                  fontSize: 14,
                                  fontWeight: '600',
                                  color: '#3b82f6',
                                }}>
                                  {t('joinOrCreate.pendingInvitations', 'You have pending invitations!')}
                                </Text>
                              </View>

                              {pendingInvitations.map((invitation) => (
                                <TouchableOpacity
                                  key={invitation.invitationId}
                                  onPress={() => handleAcceptInvitation(invitation.token)}
                                  disabled={isSubmitting}
                                  style={{
                                    backgroundColor: isDark ? COLORS.surface : COLORS.white,
                                    borderRadius: 12,
                                    padding: 12,
                                    marginBottom: 8,
                                  }}
                                >
                                  <Text style={{
                                    fontSize: 15,
                                    fontWeight: '600',
                                    color: isDark ? COLORS.white : COLORS.slate900,
                                  }}>
                                    {invitation.siteName}
                                  </Text>
                                  <Text style={{
                                    fontSize: 13,
                                    color: COLORS.textSoft,
                                    marginTop: 4,
                                  }}>
                                    {t('joinOrCreate.invitedBy', 'Invited by')}: {invitation.invitedByEmail}
                                  </Text>
                                  <Text style={{
                                    fontSize: 12,
                                    color: '#3b82f6',
                                    marginTop: 8,
                                    fontWeight: '500',
                                  }}>
                                    {t('joinOrCreate.tapToAccept', 'Tap to accept invitation')}
                                  </Text>
                                </TouchableOpacity>
                              ))}
                            </View>
                          )}

                          {/* Create Home Option */}
                          <TouchableOpacity
                            onPress={handleCreateHome}
                            style={{
                              backgroundColor: isDark ? COLORS.surface : COLORS.white,
                              borderRadius: 16,
                              padding: 20,
                              marginBottom: 16,
                              borderWidth: 1,
                              borderColor: isDark ? COLORS.border : COLORS.gray200,
                              flexDirection: 'row',
                              alignItems: 'center',
                            }}
                          >
                            <View
                              style={{
                                width: 48,
                                height: 48,
                                borderRadius: 12,
                                backgroundColor: isDark ? 'rgba(0, 209, 161, 0.15)' : 'rgba(0, 209, 161, 0.1)',
                                justifyContent: 'center',
                                alignItems: 'center',
                                marginRight: 16,
                              }}
                            >
                              <Home size={24} color={COLORS.accent} />
                            </View>
                            <View style={{ flex: 1 }}>
                              <Text
                                style={{
                                  fontSize: 16,
                                  fontWeight: '600',
                                  color: isDark ? COLORS.white : COLORS.slate900,
                                  marginBottom: 4,
                                }}
                              >
                                {t('joinOrCreate.createHome')}
                              </Text>
                              <Text
                                style={{
                                  fontSize: 14,
                                  color: isDark ? COLORS.textSoft : COLORS.slate600,
                                }}
                              >
                                {t('joinOrCreate.createHomeDesc')}
                              </Text>
                            </View>
                          </TouchableOpacity>

                          {/* Join Shared Home Option */}
                          <TouchableOpacity
                            onPress={handleJoinSharedHome}
                            style={{
                              backgroundColor: isDark ? COLORS.surface : COLORS.white,
                              borderRadius: 16,
                              padding: 20,
                              borderWidth: 1,
                              borderColor: isDark ? COLORS.border : COLORS.gray200,
                              flexDirection: 'row',
                              alignItems: 'center',
                            }}
                          >
                            <View
                              style={{
                                width: 48,
                                height: 48,
                                borderRadius: 12,
                                backgroundColor: isDark ? 'rgba(59, 130, 246, 0.15)' : 'rgba(59, 130, 246, 0.1)',
                                justifyContent: 'center',
                                alignItems: 'center',
                                marginRight: 16,
                              }}
                            >
                              <Users size={24} color="#3b82f6" />
                            </View>
                            <View style={{ flex: 1 }}>
                              <Text
                                style={{
                                  fontSize: 16,
                                  fontWeight: '600',
                                  color: isDark ? COLORS.white : COLORS.slate900,
                                  marginBottom: 4,
                                }}
                              >
                                {t('joinOrCreate.joinSharedHome')}
                              </Text>
                              <Text
                                style={{
                                  fontSize: 14,
                                  color: isDark ? COLORS.textSoft : COLORS.slate600,
                                }}
                              >
                                {t('joinOrCreate.joinSharedHomeDesc')}
                              </Text>
                            </View>
                          </TouchableOpacity>

                          {isSubmitting && (
                            <View style={{ marginTop: 16, alignItems: 'center' }}>
                              <ActivityIndicator color={COLORS.accent} />
                              <Text style={{ color: COLORS.textSoft, marginTop: 8 }}>
                                {t('joinOrCreate.acceptingInvitation', 'Accepting invitation...')}
                              </Text>
                            </View>
                          )}

                          {error && (
                            <View style={{
                              marginTop: 16,
                              padding: 12,
                              backgroundColor: isDark ? 'rgba(239, 68, 68, 0.1)' : 'rgba(239, 68, 68, 0.08)',
                              borderRadius: 8,
                            }}>
                              <Text style={{ color: '#ef4444', fontSize: 14 }}>{error}</Text>
                            </View>
                          )}
                        </View>
                      </>
                    ) : (
                      <>
                        <Text style={styles.title}>{t('joinOrCreate.enterCodeTitle')}</Text>
                        <Text style={styles.subtitle}>{t('joinOrCreate.enterCodeSubtitle')}</Text>

                        <View style={styles.formWrapper}>
                          <Input
                            label={t('joinOrCreate.invitationCode')}
                            placeholder={t('joinOrCreate.codePlaceholder')}
                            keyboardType="number-pad"
                            maxLength={6}
                            value={invitationCode}
                            onChangeText={(text) => {
                              setInvitationCode(text.replace(/[^0-9]/g, ''));
                              setError('');
                            }}
                            error={error}
                            autoFocus
                          />

                          <Button
                            title={isSubmitting ? t('common.loading') : t('joinOrCreate.acceptInvitation')}
                            onPress={() => handleAcceptInvitation()}
                            disabled={isSubmitting || !invitationCode.trim()}
                            style={styles.primaryButton}
                            textStyle={styles.primaryButtonText}
                          />

                          {isSubmitting && (
                            <View style={{ marginTop: 16, alignItems: 'center' }}>
                              <ActivityIndicator color={COLORS.accent} />
                            </View>
                          )}

                          <Button
                            title={t('common.back')}
                            onPress={handleBack}
                            variant="secondary"
                            style={[styles.secondaryButton, { marginTop: 12 }]}
                            textStyle={styles.secondaryButtonText}
                          />
                        </View>
                      </>
                    )}
                  </View>
                </Card>
              </View>
            </KeyboardAvoidingView>
          </View>
        </View>
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
