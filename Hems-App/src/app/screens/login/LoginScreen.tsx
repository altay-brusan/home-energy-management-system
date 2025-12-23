import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { JSX } from 'react';
import {
  Keyboard,
  KeyboardAvoidingView,
  Pressable,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  useSharedValue,
  withSpring,
  withTiming,
} from 'react-native-reanimated';
import LinearGradient from 'react-native-linear-gradient';
import Svg, { Path, G } from 'react-native-svg';

import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { LanguageSelector, ThemeToggle, LegalModal } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import useAuth from '../../../hooks/useAuth';
import { usePlatform } from '../../../hooks/usePlatform';
import { useAuthStore } from '../../../store/authStore';
import { useOnboardingStore } from '../../../store/onboardingStore';
import { useSitesStore } from '../../../store/sitesStore';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import type { LoginForm } from './login.schema';
import { loginSchema } from './login.schema';
import { makeLoginStyles } from './LoginScreen.styles';

type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
  ForgotPassword: undefined;
  ResetPassword: { email: string };
  SignUp: undefined;
  Onboarding: undefined;
  Home: undefined;
};

// GRP Logo Component
const GRPLogo = ({ width = 180, height = 45, isDark }: { width?: number; height?: number; isDark: boolean }) => {
  const fillColor = isDark ? '#FFFFFF' : '#13110c';
  const accentColor = isDark ? '#FFFFFF' : '#00a270';

  return (
    <Svg width={width} height={height} viewBox="0 0 272.31 62.57">
      <G>
        <Path fill={fillColor} d="M224.05,15.6v.54c0,1.18-.41,2.16-1.22,2.94-.81.78-1.89,1.17-3.22,1.17s-2.52-.45-3.42-1.37c-.9-.91-1.35-2.02-1.35-3.33s.45-2.42,1.35-3.33c.9-.91,2.03-1.37,3.4-1.37.84,0,1.6.2,2.3.6.7.4,1.24.93,1.62,1.59l-1.04.61c-.26-.49-.65-.89-1.17-1.18-.52-.3-1.1-.45-1.73-.45-1.04,0-1.88.34-2.55,1.02-.66.68-1,1.52-1,2.52s.33,1.84,1,2.51c.67.67,1.53,1.01,2.59,1.01.91,0,1.64-.22,2.2-.67.56-.44.9-1.02,1.02-1.73h-3.33v-1.09h4.53Z"/>
        <Path fill={fillColor} d="M232.96,20.09l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82s.83,1.21.83,1.99c0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM228.79,12.16v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M238.77 18.95 242.97 18.95 242.97 20.09 237.56 20.09 237.56 11.04 242.91 11.04 242.91 12.17 238.77 12.17 238.77 14.96 242.58 14.96 242.58 16.08 238.77 16.08 238.77 18.95"/>
        <Path fill={fillColor} d="M247.69 18.95 251.9 18.95 251.9 20.09 246.49 20.09 246.49 11.04 251.83 11.04 251.83 12.17 247.69 12.17 247.69 14.96 251.51 14.96 251.51 16.08 247.69 16.08 247.69 18.95"/>
        <Path fill={fillColor} d="M261.05 11.04 262.25 11.04 262.25 20.09 261.28 20.09 256.62 13.33 256.62 20.09 255.42 20.09 255.42 11.04 256.35 11.04 261.05 17.84 261.05 11.04"/>
        <Path fill={fillColor} d="M218.61,26.56c.84,0,1.53.28,2.09.83s.83,1.25.83,2.08-.28,1.51-.83,2.07c-.56.56-1.25.84-2.09.84h-2.08v3.23h-1.2v-9.06h3.29ZM218.61,31.26c.5,0,.91-.17,1.24-.51.33-.34.49-.77.49-1.27s-.16-.94-.49-1.28c-.33-.34-.74-.5-1.24-.5h-2.08v3.57h2.08Z"/>
        <Path fill={fillColor} d="M232.5,34.42c-.91.91-2.02,1.37-3.33,1.37s-2.42-.46-3.33-1.37c-.91-.91-1.36-2.02-1.36-3.33s.45-2.42,1.36-3.33c.91-.91,2.01-1.37,3.33-1.37s2.42.45,3.33,1.37c.91.91,1.36,2.02,1.36,3.33s-.46,2.41-1.36,3.33M226.68,33.6c.67.68,1.5,1.02,2.49,1.02s1.82-.34,2.49-1.02c.67-.68,1-1.51,1-2.5s-.33-1.84-1-2.52c-.67-.68-1.5-1.02-2.49-1.02s-1.82.34-2.49,1.02c-.67.68-1,1.52-1,2.52s.33,1.83,1,2.5"/>
        <Path fill={fillColor} d="M238.87 35.62 236.33 26.56 237.6 26.56 239.67 34.02 241.87 26.56 243.09 26.56 245.29 34.02 247.36 26.56 248.56 26.56 246.03 35.62 244.63 35.62 242.47 28.37 240.32 35.62 238.87 35.62"/>
        <Path fill={fillColor} d="M253.19 34.48 257.4 34.48 257.4 35.62 251.99 35.62 251.99 26.56 257.33 26.56 257.33 27.7 253.19 27.7 253.19 30.48 257.01 30.48 257.01 31.61 253.19 31.61 253.19 34.48"/>
        <Path fill={fillColor} d="M266.29,35.62l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82.55.55.83,1.21.83,1.99,0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM262.12,27.69v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M216.52 50.01 220.73 50.01 220.73 51.15 215.32 51.15 215.32 42.09 220.66 42.09 220.66 43.23 216.52 43.23 216.52 46.01 220.34 46.01 220.34 47.14 216.52 47.14 216.52 50.01"/>
        <Path fill={fillColor} d="M229.88 42.09 231.08 42.09 231.08 51.15 230.11 51.15 225.45 44.38 225.45 51.15 224.25 51.15 224.25 42.09 225.18 42.09 229.88 48.89 229.88 42.09"/>
        <Path fill={fillColor} d="M236.3 50.01 240.5 50.01 240.5 51.15 235.09 51.15 235.09 42.09 240.44 42.09 240.44 43.23 236.3 43.23 236.3 46.01 240.11 46.01 240.11 47.14 236.3 47.14 236.3 50.01"/>
        <Path fill={fillColor} d="M249.39,51.15l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82s.83,1.21.83,1.99c0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM245.22,43.22v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M262.69,46.66v.54c0,1.18-.41,2.16-1.22,2.94-.81.78-1.89,1.17-3.22,1.17s-2.52-.45-3.42-1.37c-.9-.91-1.35-2.02-1.35-3.33s.45-2.42,1.35-3.33c.9-.91,2.03-1.37,3.4-1.37.84,0,1.6.2,2.3.6.7.4,1.24.93,1.62,1.59l-1.04.61c-.26-.49-.65-.89-1.17-1.18-.52-.3-1.1-.45-1.73-.45-1.04,0-1.88.34-2.55,1.02-.66.68-1,1.52-1,2.52s.33,1.84,1,2.51c.67.67,1.53,1.01,2.59,1.01.91,0,1.64-.22,2.2-.67.56-.44.9-1.02,1.02-1.73h-3.33v-1.09h4.53Z"/>
        <Path fill={fillColor} d="M272.31 42.09 269.1 47.49 269.1 51.15 267.89 51.15 267.89 47.47 264.7 42.09 266.04 42.09 268.51 46.34 270.96 42.09 272.31 42.09"/>
        <Path fill={fillColor} d="M155.77,30.93c1.97-1.98,2.95-4.72,2.95-8.2,0-3.74-1.12-6.63-3.37-8.65-2.25-2.02-5.41-3.03-9.49-3.03h-17.46v23.36h3.93V14.42h12.97c3.03,0,5.35.69,6.96,2.08,1.61,1.39,2.41,3.46,2.41,6.23s-.81,4.85-2.41,6.23c-1.61,1.39-3.93,2.08-6.96,2.08h-2.4v3.31l.04.06,11.4,16.73h4.6l-11.51-16.79c3.59-.3,6.37-1.44,8.34-3.42"/>
        <Path fill={fillColor} d="M195.86,14.14c-2.25-2.06-5.41-3.09-9.49-3.09h-16.34v40.09h3.93V14.42h11.79c3.07,0,5.41.7,7.02,2.11,1.61,1.4,2.41,3.53,2.41,6.37s-.81,4.97-2.41,6.37c-1.61,1.4-3.95,2.1-7.02,2.1h-2.64v3.37h3.25c4.08,0,7.24-1.03,9.49-3.09,2.25-2.06,3.37-4.98,3.37-8.76s-1.12-6.7-3.37-8.76"/>
        <Path fill={fillColor} d="M99.6,48.21c-9.4,0-17.04-7.64-17.04-17.04s7.64-17.04,17.04-17.04c.09,0,.19,0,.28,0v-3.91c-.09,0-.19,0-.28,0-11.55,0-20.95,9.4-20.95,20.95s9.4,20.95,20.95,20.95c10.51,0,19.23-7.78,20.72-17.88h-3.97c-1.45,7.93-8.41,13.97-16.76,13.97"/>
        <Path fill={fillColor} d="M99.88 29.13 109.67 29.13 109.67 33.06 99.88 33.06 99.88 29.13"/>
        <Path fill={accentColor} d="M31.28,0C14.01,0,0,14.01,0,31.28s14.01,31.28,31.28,31.28,31.28-14.01,31.28-31.28S48.56,0,31.28,0M31.4,52.23c-11.55,0-20.95-9.4-20.95-20.95S19.85,10.34,31.4,10.34c.09,0,.19,0,.28,0v3.91c-.09,0-.19,0-.28,0-9.4,0-17.04,7.64-17.04,17.04s7.64,17.04,17.04,17.04c8.35,0,15.31-6.03,16.76-13.97h3.97c-1.49,10.1-10.21,17.88-20.72,17.88M41.46,33.19h-9.79v-3.93h9.79v3.93Z"/>
      </G>
    </Svg>
  );
};

export default function LoginScreen (): JSX.Element {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { login, loginLoading, loginError, clearLoginError, isAuthenticated } = useAuth();
  const user = useAuthStore(s => s.user);
  const hasOnboarded = useOnboardingStore(s => s.hasOnboarded);
  const { getUserSites, selectSiteById } = useSitesStore();
  const { t } = useTranslation();

  const { isIOS } = usePlatform();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = React.useMemo(
    () => makeLoginStyles(COLORS, isDark),
    [COLORS, isDark],
  );

  const [agreed, setAgreed] = React.useState<boolean>(false);
  const [legalModalVisible, setLegalModalVisible] = React.useState(false);
  const [legalModalType, setLegalModalType] = React.useState<'privacy' | 'terms'>('terms');

  const openLegalModal = (type: 'privacy' | 'terms') => {
    setLegalModalType(type);
    setLegalModalVisible(true);
  };

  const scale = useSharedValue(1);
  const rotation = useSharedValue(0);

  const onPressFancy = () => {
    scale.value = withSpring(1.03, {}, () => (scale.value = withSpring(1)));
    rotation.value = withTiming(
      360,
      { duration: 420 },
      () => (rotation.value = 0),
    );
  };

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "administrator@localhost", password: "***REMOVED***" },
  });

  React.useEffect(() => {
    if (!isAuthenticated || !user) return;

    const checkHomesAndNavigate = async () => {
      try {
        const isAdminUser = user?.roles?.includes('Administrator');
        if (isAdminUser) {
          navigation.replace('MainTabs');
        } else {
          // Normal kullanıcı için siteleri kontrol et
          const sitesDashboard = await getUserSites();
          const sites = sitesDashboard?.siteList || [];

          if (sites.length === 1) {
            // Tek site varsa direkt seç ve Dashboard'a git
            const singleSite = sites[0];
            await selectSiteById(singleSite.id);
            navigation.replace('MainTabs');
          } else {
            // Birden fazla site veya hiç site yoksa Home'a git
            navigation.replace('Home');
          }
        }
      } catch (error) {
        navigation.replace('Home');
      }
    };

    checkHomesAndNavigate();
  }, [isAuthenticated, user, navigation, getUserSites, selectSiteById]);

  const onSubmit = (values: LoginForm) => {
    if (!agreed) return;
    onPressFancy();
    void login(values);
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      {/* Gradient Background - Only for light mode */}
      {!isDark && (
        <LinearGradient
          colors={['rgba(118, 192, 253, 0.48)', 'rgba(243, 243, 245, 1)']}
          start={{ x: 0.5, y: 0 }}
          end={{ x: 0.5, y: 1 }}
          style={styles.gradient}
        />
      )}

      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <KeyboardAvoidingView
          style={styles.root}
          behavior={isIOS ? 'padding' : undefined}
        >
          {/* Top Bar */}
          <View style={styles.topBar}>
            <ThemeToggle style={styles.themeToggle} />
            <View style={styles.regionWrap}>
              <LanguageSelector />
            </View>
          </View>

          <ScrollView
            contentContainerStyle={styles.scrollContent}
            keyboardShouldPersistTaps="handled"
            keyboardDismissMode="on-drag"
            showsVerticalScrollIndicator={false}
          >
            {/* Logo */}
            <View style={styles.logoContainer}>
              <GRPLogo width={200} height={50} isDark={isDark} />
            </View>

            {/* Form Card */}
            <View style={styles.formCard}>
              <Text style={styles.welcomeText}>{t('auth.welcomeBack') || 'Welcome Back'}</Text>
              <Text style={styles.subtitleText}>{t('auth.loginSubtitle') || 'Sign in to continue'}</Text>

              <View style={styles.form}>
                <Controller
                  control={control}
                  name="email"
                  render={({ field: { onChange, value } }) => (
                    <Input
                      placeholder={t('auth.emailPlaceholder')}
                      keyboardType="email-address"
                      autoCapitalize="none"
                      autoCorrect={false}
                      autoComplete="email"
                      value={value}
                      onChangeText={(text) => {
                        onChange(text.toLowerCase());
                        if (loginError) clearLoginError();
                      }}
                      error={errors.email?.message}
                    />
                  )}
                />
                <Controller
                  control={control}
                  name="password"
                  render={({ field: { onChange, value } }) => (
                    <Input
                      placeholder={t('auth.passwordPlaceholder')}
                      secureTextEntry
                      autoCorrect={false}
                      autoComplete="password"
                      value={value}
                      onChangeText={(text) => {
                        onChange(text);
                        if (loginError) clearLoginError();
                      }}
                      error={errors.password?.message}
                    />
                  )}
                />

                <Pressable
                  onPress={() => navigation.navigate('ForgotPassword')}
                  style={styles.forgotContainer}
                >
                  <Text style={styles.forgot}>{t('auth.forgotPassword')}</Text>
                </Pressable>

                <Pressable
                  onPress={() => setAgreed(v => !v)}
                  accessibilityRole="checkbox"
                  accessibilityState={{ checked: agreed }}
                  style={styles.tosRow}
                >
                  <View
                    style={[
                      styles.checkbox,
                      agreed && styles.checkboxChecked,
                    ]}
                  >
                    {agreed && <Text style={styles.checkIcon}>✓</Text>}
                  </View>
                  <Text style={styles.tosText}>
                    {t('auth.agreeToTerms')}{' '}
                    <Text style={styles.tosLink} onPress={() => openLegalModal('terms')}>
                      {t('auth.termsOfService')}
                    </Text>{' '}
                    {t('auth.and')}{' '}
                    <Text style={styles.tosLink} onPress={() => openLegalModal('privacy')}>
                      {t('auth.privacyPolicy')}
                    </Text>
                  </Text>
                </Pressable>

                <Pressable onPress={onPressFancy} disabled={!agreed}>
                  <Button
                    title={
                      loginLoading || isSubmitting
                        ? t('auth.signingIn')
                        : t('auth.loginButton')
                    }
                    onPress={handleSubmit(onSubmit)}
                    variant="primary"
                    style={[
                      styles.primaryBtn,
                      !agreed && styles.loginBtnDisabled,
                    ]}
                    disabled={!agreed || loginLoading || isSubmitting}
                  />
                </Pressable>

                <Pressable onPress={() => navigation.navigate('SignUp')} style={styles.signupContainer}>
                  <Text style={styles.signupLink}>
                    {t('auth.dontHaveAccount')} <Text style={styles.signupHighlight}>{t('auth.signUp')}</Text>
                  </Text>
                </Pressable>
              </View>
            </View>
          </ScrollView>
        </KeyboardAvoidingView>
      </TouchableWithoutFeedback>

      <LegalModal
        visible={legalModalVisible}
        type={legalModalType}
        onClose={() => setLegalModalVisible(false)}
      />
    </SafeAreaView>
  );
}
