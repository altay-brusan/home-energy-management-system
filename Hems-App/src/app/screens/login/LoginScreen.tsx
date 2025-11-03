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
  View
} from 'react-native';
import {
  useSharedValue,
  withSpring,
  withTiming,
} from 'react-native-reanimated';

import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Card, LanguageSelector, ThemeToggle } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import useAuth from '../../../hooks/useAuth';
import { usePlatform } from '../../../hooks/usePlatform';
import { useAuthStore } from '../../../store/authStore';
import { useOnboardingStore } from '../../../store/onboardingStore';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';
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

export default function LoginScreen (): JSX.Element {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { login, loginLoading, loginError, clearLoginError, isAuthenticated } = useAuth();
  const user = useAuthStore(s => s.user);
  const hasOnboarded = useOnboardingStore(s => s.hasOnboarded);
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
        const isAdminUser = user?.roles?.includes('Administrator');if (isAdminUser) {navigation.replace('MainTabs');
        } else {navigation.replace('Home');
        }
      } catch (error) {navigation.replace('Home');
      }
    };

    checkHomesAndNavigate();
  }, [isAuthenticated, user, navigation]);

  const onSubmit = (values: LoginForm) => {
    if (!agreed) return;
    onPressFancy();
    void login(values);
  };

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <KeyboardAvoidingView
          style={styles.root}
          behavior={isIOS ? 'padding' : undefined}
        >
          <View style={styles.topBar}>
            <ThemeToggle style={styles.themeToggle} />
            <View style={styles.regionWrap}>
              <LanguageSelector />
            </View>
          </View>

          <View style={styles.centerWrap}>
            <Card variant="elevated" padding={24} style={styles.card}>
              <ScrollView
                contentContainerStyle={styles.scrollContent}
                keyboardShouldPersistTaps="handled"
                keyboardDismissMode="on-drag"
                showsVerticalScrollIndicator={false}
              >
                <View style={styles.cardInner}>
                  <Text style={styles.brand}>HEMS</Text>
                  <View style={styles.formWrap}>
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
                      >
                        <Text style={styles.forgot}>{t('auth.forgotPassword')}</Text>
                      </Pressable>
                      <Pressable onPress={() => navigation.navigate('SignUp')}>
                        <Text style={styles.signupLink}>
                          {t('auth.dontHaveAccount')} <Text style={styles.signupHighlight}>{t('auth.signUp')}</Text>
                        </Text>
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
                          <Text style={styles.tosLink}>{t('auth.termsOfService')}</Text>{' '}
                          {t('auth.and')} <Text style={styles.tosLink}>{t('auth.privacyPolicy')}</Text>
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
                    </View>
                  </View>
                </View>
              </ScrollView>
            </Card>
          </View>

          <View style={styles.bgTopShade} pointerEvents="none" />
          <View style={styles.bgBottomShade} pointerEvents="none" />
        </KeyboardAvoidingView>
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
