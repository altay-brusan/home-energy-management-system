import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useCallback, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import {
  Keyboard,
  KeyboardAvoidingView,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';

import { Card, ThemeToggle } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import QrScannerModal from '../../../components/QrScannerModal';
import { usePlatform } from '../../../hooks/usePlatform';
import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getOnboardingStyles } from './Onboarding.styles';
import { deviceSetupSchema, type DeviceSetupForm } from './device.schema';
import { useOnboardingStore } from '../../../store/onboardingStore';

type RootStackParamList = {
  Home: undefined;
  DeviceSetup: undefined;
  MainTabs: undefined;
};

export default function DeviceSetupScreen() {
  const { t } = useTranslation();
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { setDevice, completeOnboarding } = useOnboardingStore();
  const { isIOS } = usePlatform();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getOnboardingStyles(COLORS, isDark), [COLORS, isDark]);

  const [isQrScannerVisible, setIsQrScannerVisible] = useState(false);

  const {
    control,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<DeviceSetupForm>({
    resolver: zodResolver(deviceSetupSchema),
    defaultValues: {
      deviceName: '',
      serialNumber: '',
    },
  });

  const onSubmit = useCallback(
    (values: DeviceSetupForm) => {
      setDevice({ name: values.deviceName, serialNumber: values.serialNumber });
      completeOnboarding();
      navigation.navigate('Home'); 
    },
    [completeOnboarding, navigation, setDevice],
  );

  const handleQrScan = useCallback(
    (code: string) => {
      setValue('serialNumber', code);
      setIsQrScannerVisible(false);
    },
    [setValue],
  );

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <View style={styles.root}>
          <View style={styles.topBar}>
            <ThemeToggle style={styles.themeToggle} />
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
                  <View style={styles.inner}>
                    <Text style={styles.title}>{t('deviceSetup.title')}</Text>
                    <Text style={styles.subtitle}>{t('deviceSetup.subtitle')}</Text>

                    <View style={styles.formWrapper}>
                      <Controller
                        control={control}
                        name="deviceName"
                        render={({ field }) => (
                          <Input
                            label={t('deviceSetup.deviceName')}
                            placeholder={t('deviceSetup.deviceNamePlaceholder')}
                            autoCapitalize="words"
                            value={field.value}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.deviceName?.message}
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="serialNumber"
                        render={({ field }) => (
                          <Input
                            label={t('deviceSetup.serialNumber')}
                            placeholder={t('deviceSetup.serialNumberPlaceholder')}
                            autoCapitalize="characters"
                            autoCorrect={false}
                            value={field.value}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.serialNumber?.message}
                            onSubmitEditing={handleSubmit(onSubmit)}
                          />
                        )}
                      />
                      <Button
                        title={t('deviceSetup.scanQr')}
                        onPress={() => setIsQrScannerVisible(true)}
                        variant="secondary"
                        style={styles.secondaryButton}
                        textStyle={styles.secondaryButtonText}
                      />
                      <Button
                        title={
                          isSubmitting
                            ? t('deviceSetup.saving')
                            : t('deviceSetup.finish')
                        }
                        onPress={handleSubmit(onSubmit)}
                        disabled={isSubmitting}
                        style={styles.primaryButton}
                        textStyle={styles.primaryButtonText}
                      />
                      <Button
                        title={t('deviceSetup.goBack')}
                        onPress={() => navigation.goBack()}
                        variant="secondary"
                        style={[styles.secondaryButton, { marginTop: 12 }]}
                        textStyle={styles.secondaryButtonText}
                      />
                    </View>
                  </View>
                </Card>
              </ScrollView>
            </KeyboardAvoidingView>
          </View>
        </View>
      </TouchableWithoutFeedback>
      <QrScannerModal
        visible={isQrScannerVisible}
        onClose={() => setIsQrScannerVisible(false)}
        onScan={handleQrScan}
      />
    </MainLayout>
  );
}
