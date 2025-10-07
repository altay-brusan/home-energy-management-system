import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useCallback } from 'react';
import { useForm } from 'react-hook-form';
import {
  Keyboard,
  KeyboardAvoidingView,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View,
} from 'react-native';

import { Card, ThemeToggle, LanguageSelector } from '../../../components';
import { useTranslation } from 'react-i18next';
import { usePlatform } from '../../../hooks/usePlatform';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';

import { SignUpForm, signupSchema } from './signup.schema';

import SignUpFormFields from './SignUpForm';
import { getSignUpStyles } from './SignUpScreen.styles';

type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
  ForgotPassword: undefined;
  SignUp: undefined;
  Onboarding: undefined;
};

export default function SignUpScreen() {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getSignUpStyles(COLORS, isDark);
  const { isIOS } = usePlatform();

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<SignUpForm>({
    resolver: zodResolver(signupSchema),
    defaultValues: { name: '', email: '', password: '', confirmPassword: '' },
  });

  const handleNavigateBack = useCallback(() => {
    navigation.goBack();
  }, [navigation]);

  const onSubmit = useCallback(
    async (values: SignUpForm) => {
      console.log('Sign up values:', values);
      reset();
      navigation.goBack();
    },
    [navigation, reset],
  );

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <View style={styles.root}>
          <View style={styles.topBar}>
            <ThemeToggle style={styles.themeToggle} />
            <View style={styles.regionWrap}>
              <LanguageSelector />
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
              <ScrollView
                contentContainerStyle={styles.scrollContent}
                keyboardShouldPersistTaps="handled"
                keyboardDismissMode="on-drag"
                showsVerticalScrollIndicator={false}
                bounces={false}
                scrollEventThrottle={16}
                automaticallyAdjustKeyboardInsets={isIOS}
              >
                <Card variant="elevated" padding={24} style={styles.card}>
                  <View style={styles.cardInner}>
                    <Text style={styles.brand}>Sign Up</Text>
                    <SignUpFormFields
                      control={control}
                      errors={errors}
                      isSubmitting={isSubmitting}
                      onSubmit={handleSubmit(onSubmit)}
                      onGoBack={handleNavigateBack}
                      styles={styles}
                    />
                  </View>
                </Card>
              </ScrollView>
            </KeyboardAvoidingView>
          </View>
        </View>
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
