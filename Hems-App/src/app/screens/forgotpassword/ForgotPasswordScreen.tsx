import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { JSX } from 'react';
import { Controller, useForm } from 'react-hook-form';
import {
  Keyboard,
  KeyboardAvoidingView,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View
} from 'react-native';

import { useTranslation } from 'react-i18next';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import { usePlatform } from '../../../hooks/usePlatform';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';
import { getForgotPasswordStyles } from './ForgotPasswordScreen.styles';

import { Card, LanguageSelector, ThemeToggle } from '../../../components';
import useForgotPassword from '../../../hooks/useForgotPassword';
import { ForgotPasswordForm, createForgotPasswordSchema } from './forgotPassword.schema';
import { showToast } from '../../../utils/toast';

type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
  ForgotPassword: undefined;
  ResetPassword: { email: string };
};

export default function ForgotPasswordScreen (): JSX.Element {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { t } = useTranslation();
  const { forgotPassword, forgotPasswordLoading, forgotPasswordError, forgotPasswordSuccess } = useForgotPassword();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getForgotPasswordStyles(COLORS, isDark);
  const { isIOS } = usePlatform();

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<ForgotPasswordForm>({
    resolver: zodResolver(createForgotPasswordSchema(t)),
    defaultValues: { email: '' },
  });

  const onSubmit = async (values: ForgotPasswordForm) => {
    try {
      await forgotPassword({ email: values.email });
      reset();

      showToast({
        title: t('common.success'),
        message: t('auth.resetCodeSent'),
        type: 'success',
      });

      navigation.navigate('ResetPassword', { email: values.email });
    } catch (error) {
      // Error is handled by useForgotPassword hook
    }
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
                  <Text style={styles.brand}>{t('auth.forgotPasswordTitle')}</Text>
                  <Text style={styles.subtitle}>{t('auth.forgotPasswordSubtitle')}</Text>
                  <View style={styles.formWrap}>
                    <View style={styles.form}>
                      <Controller
                        control={control}
                        name="email"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            label={t('auth.email')}
                            placeholder={t('auth.emailPlaceholder')}
                            keyboardType="email-address"
                            autoCapitalize="none"
                            autoCorrect={false}
                            autoComplete="email"
                            value={value}
                            onChangeText={(text) => onChange(text.toLowerCase())}
                            error={errors.email?.message}
                          />
                        )}
                      />
                      {forgotPasswordSuccess && (
                        <Text
                          style={{
                            color: COLORS.success,
                            marginTop: 8,
                            textAlign: 'center',
                            fontWeight: '600',
                          }}
                        >
                          {t('auth.resetLinkSent')}
                        </Text>
                      )}
                      <Button
                        title={forgotPasswordLoading ? t('auth.sending') : t('auth.sendResetCode')}
                        onPress={handleSubmit(onSubmit)}
                        variant="primary"
                        style={[styles.primaryBtn, { marginTop: 8 }]}
                        disabled={forgotPasswordLoading || isSubmitting}
                      />
                      <Button
                        title={t('auth.backToLogin')}
                        onPress={() => navigation.goBack()}
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
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
