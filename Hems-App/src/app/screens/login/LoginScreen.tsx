import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { JSX } from 'react';
import {
  KeyboardAvoidingView,
  Pressable,
  ScrollView,
  Text,
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
  HomeSetup: undefined;
};

// Netzr Logo Component
const NetzrLogo = ({ width = 180, height = 56, isDark }: { width?: number; height?: number; isDark: boolean }) => {
  const fillColor = isDark ? '#FFFFFF' : '#2f52a0';

  return (
    <Svg width={width} height={height} viewBox="0 0 420.25 131.85">
      <G>
        <Path fill={fillColor} d="M91.92,131.85L23.13,35.78v96.07H0V0h23.72l67.41,93.5V0h23.13v131.85h-22.34Z"/>
        <Path fill={fillColor} d="M173.8,36.1c27.32,0,44.85,20.92,44.85,49.56v4.71h-70.29c1.51,13.76,11.87,25.06,29.02,25.06,8.86,0,19.41-3.58,25.82-9.99l9.05,13c-9.05,8.67-22.43,13.19-36.94,13.19-27.32,0-47.68-19.03-47.68-47.87,0-26.38,19.22-47.68,46.17-47.68ZM148.17,76.43h51.26c-.38-10.74-7.73-24.12-25.63-24.12-16.96,0-24.69,13-25.63,24.12Z"/>
        <Path fill={fillColor} d="M236.22,100.35v-45.79h-6.45l-11.12-18.19h17.57V10.28h20.76v26.09h19.37v18.19h-19.37v44.39c0,8.23,6.67,14.91,14.91,14.91h4.46s0,17.99,0,17.99h-8.63c-17.4,0-31.5-14.1-31.5-31.5Z"/>
        <Path fill={fillColor} d="M291.16,113.86l45.43-59.3h-45.43v-18.19h70.83s0,18.19,0,18.19l-45.37,59.3h45.37v17.99h-70.83v-17.99Z"/>
        <Path fill={fillColor} d="M374.87,131.85v-65.67c0-16.46,13.34-29.8,29.8-29.8h15.58v18.19h-12.41c-6.75,0-12.22,5.47-12.22,12.22v65.07h-20.76Z"/>
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
    defaultValues: { email: "", password: "" },
  });

  React.useEffect(() => {
    if (!isAuthenticated || !user) return;

    const checkHomesAndNavigate = async () => {
      try {
        const isAdminUser = user?.roles?.includes('Administrator');
        console.log('🔐 [Login] User roles:', user?.roles, 'isAdmin:', isAdminUser);

        if (isAdminUser) {
          console.log('🔐 [Login] Admin user - going to MainTabs');
          navigation.reset({
            index: 0,
            routes: [{ name: 'MainTabs' }],
          });
        } else {
          // Normal kullanıcı için siteleri kontrol et
          console.log('🔐 [Login] Fetching user sites...');
          const sitesDashboard = await getUserSites();
          const sites = sitesDashboard?.siteList || [];
          console.log('🔐 [Login] Sites count:', sites.length, sites);

          if (sites.length === 0) {
            // Hiç site yoksa HomeSetup'a git
            console.log('🔐 [Login] No sites - navigating to HomeSetup');
            navigation.reset({
              index: 0,
              routes: [{ name: 'HomeSetup' }],
            });
          } else if (sites.length === 1) {
            // Tek site varsa direkt seç ve Dashboard'a git
            console.log('🔐 [Login] Single site - selecting and going to MainTabs');
            const singleSite = sites[0];
            await selectSiteById(singleSite.id);
            navigation.reset({
              index: 0,
              routes: [{ name: 'MainTabs' }],
            });
          } else {
            // Birden fazla site varsa Home'a git (site seçimi için)
            console.log('🔐 [Login] Multiple sites - going to Home');
            navigation.reset({
              index: 0,
              routes: [{ name: 'Home' }],
            });
          }
        }
      } catch (error) {
        console.log('🔐 [Login] Error fetching sites:', error);
        // Hata durumunda HomeSetup'a git (güvenli taraf)
        navigation.reset({
          index: 0,
          routes: [{ name: 'HomeSetup' }],
        });
      }
    };

    checkHomesAndNavigate();
  }, [isAuthenticated, user, navigation, getUserSites, selectSiteById]);

  const onSubmit = (values: LoginForm) => {
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
          bounces={true}
          alwaysBounceVertical={true}
        >
            {/* Logo */}
            <View style={styles.logoContainer}>
              <NetzrLogo width={150} height={47} isDark={isDark} />
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

                <Pressable onPress={onPressFancy}>
                  <Button
                    title={
                      loginLoading || isSubmitting
                        ? t('auth.signingIn')
                        : t('auth.loginButton')
                    }
                    onPress={handleSubmit(onSubmit)}
                    variant="primary"
                    style={styles.primaryBtn}
                    disabled={loginLoading || isSubmitting}
                  />
                </Pressable>

                {/* Legal Links */}
                <View style={styles.legalLinksContainer}>
                  <Text style={styles.legalText}>
                    {t('auth.byLoggingIn', 'By logging in, you agree to our')}{' '}
                  </Text>
                  <View style={styles.legalLinksRow}>
                    <Text style={styles.tosLink} onPress={() => openLegalModal('terms')}>
                      {t('auth.termsOfService')}
                    </Text>
                    <Text style={styles.legalText}> {t('auth.and')} </Text>
                    <Text style={styles.tosLink} onPress={() => openLegalModal('privacy')}>
                      {t('auth.privacyPolicy')}
                    </Text>
                  </View>
                </View>

                <Pressable onPress={() => navigation.navigate('SignUp')} style={styles.signupContainer}>
                  <Text style={styles.signupLink}>
                    {t('auth.dontHaveAccount')} <Text style={styles.signupHighlight}>{t('auth.signUp')}</Text>
                  </Text>
                </Pressable>
              </View>
            </View>
        </ScrollView>
      </KeyboardAvoidingView>

      <LegalModal
        visible={legalModalVisible}
        type={legalModalType}
        onClose={() => setLegalModalVisible(false)}
      />
    </SafeAreaView>
  );
}
