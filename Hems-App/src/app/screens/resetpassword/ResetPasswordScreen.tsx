import { zodResolver } from '@hookform/resolvers/zod';
import { RouteProp, useNavigation, useRoute } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { JSX } from 'react';
import { Controller, useForm } from 'react-hook-form';
import {
  KeyboardAvoidingView,
  ScrollView,
  Text,
  View,
} from 'react-native';

import { useTranslation } from 'react-i18next';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import { usePlatform } from '../../../hooks/usePlatform';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';
import { getResetPasswordStyles } from './ResetPasswordScreen.styles';

import { Card, LanguageSelector, ThemeToggle } from '../../../components';
import useResetPassword from '../../../hooks/useResetPassword';
import { showToast } from '../../../utils/toast';
import { ResetPasswordForm, createResetPasswordSchema } from './resetPassword.schema';

type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
  ForgotPassword: undefined;
  ResetPassword: { email: string };
};

type ResetPasswordScreenRouteProp = RouteProp<RootStackParamList, 'ResetPassword'>;

export default function ResetPasswordScreen (): JSX.Element {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const route = useRoute<ResetPasswordScreenRouteProp>();
  const { email } = route.params;
  const { t } = useTranslation();
  const { resetPassword, resetPasswordLoading, resetPasswordError, resetPasswordSuccess } = useResetPassword();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getResetPasswordStyles(COLORS, isDark);
  const { isIOS } = usePlatform();

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<ResetPasswordForm>({
    resolver: zodResolver(createResetPasswordSchema(t)),
    defaultValues: { resetCode: '', newPassword: '', confirmPassword: '' },
  });

  const onSubmit = async (values: ResetPasswordForm) => {
    try {
      await resetPassword({
        email,
        resetCode: values.resetCode,
        newPassword: values.newPassword,
      });
      reset();
      showToast({
        title: t('common.success'),
        message: t('auth.passwordResetSuccess'),
        type: 'success',
      });
      navigation.navigate('Login');
    } catch (error) {}
  };

  return (
    <MainLayout>
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
              bounces={true}
              alwaysBounceVertical={true}
            >
                <View style={styles.cardInner}>
                  <Text style={styles.brand}>{t('auth.resetPasswordTitle')}</Text>
                  <Text style={styles.subtitle}>{t('auth.resetPasswordSubtitle', { email })}</Text>
                  <View style={styles.formWrap}>
                    <View style={styles.form}>
                      <Controller
                        control={control}
                        name="resetCode"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            label={t('auth.resetCode')}
                            placeholder={t('auth.resetCodePlaceholder')}
                            autoCapitalize="none"
                            autoCorrect={false}
                            value={value}
                            onChangeText={onChange}
                            error={errors.resetCode?.message}
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="newPassword"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            label={t('auth.newPassword')}
                            placeholder={t('auth.newPasswordPlaceholder')}
                            secureTextEntry
                            autoCorrect={false}
                            autoComplete="password"
                            textContentType="newPassword"
                            value={value}
                            onChangeText={onChange}
                            error={errors.newPassword?.message}
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="confirmPassword"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            label={t('auth.confirmPassword')}
                            placeholder={t('auth.confirmPasswordPlaceholder')}
                            secureTextEntry
                            autoCorrect={false}
                            autoComplete="password"
                            textContentType="password"
                            value={value}
                            onChangeText={onChange}
                            error={errors.confirmPassword?.message}
                          />
                        )}
                      />
                      {resetPasswordSuccess && (
                        <Text style={styles.successText}>
                          {t('auth.passwordResetSuccess')}
                        </Text>
                      )}
                      <Button
                        title={resetPasswordLoading ? t('auth.resetting') : t('auth.resetPasswordButton')}
                        onPress={handleSubmit(onSubmit)}
                        variant="primary"
                        style={[styles.primaryBtn, { marginTop: 8 }]}
                        disabled={resetPasswordLoading || isSubmitting}
                      />
                      <Button
                        title={t('auth.backToLogin')}
                        onPress={() => navigation.navigate('Login')}
                        style={[styles.loginBtn, { marginTop: 8 }]}
                        textStyle={styles.loginText}
                      />
                    </View>
                  </View>
                </View>
              </ScrollView>
            </Card>
          </View>
        <View style={styles.bgTopShade} pointerEvents="none" />
        <View style={styles.bgBottomShade} pointerEvents="none" />
      </KeyboardAvoidingView>
    </MainLayout>
  );
}

