import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  KeyboardAvoidingView,
  TouchableWithoutFeedback,
  Keyboard,
  Platform,
  Alert,
  SafeAreaView,
} from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Card, Button, BackButton } from '../../../components';
import Input from '../../../components/forms/Input';
import QrScannerModal from '../../../components/QrScannerModal';
import { useThemeStore } from '../../../store/themeStore';
import { useSitesStore } from '../../../store/sitesStore';
import { setupsService } from '../../../api/services/setupsService';
import { getThemeColors } from '../../../theme/constants';
import { SPACING } from '../../../theme/constants';
import { showToast } from '../../../utils/toast';

type RootStackParamList = {
  MainTabs: undefined;
  DeviceAdd: { siteId: string };
};

const deviceAddSchema = z.object({
  setupName: z
    .string()
    .min(2, 'Setup name must be at least 2 characters')
    .max(64, 'Setup name is too long'),
  deviceSerialNumber: z
    .string()
    .min(6, 'Serial number must be at least 6 characters')
    .max(40, 'Serial number is too long'),
});

type DeviceAddForm = z.infer<typeof deviceAddSchema>;

interface DeviceAddScreenProps {
  route: {
    params: {
      siteId: string;
    };
  };
}

export default function DeviceAddScreen({ route }: DeviceAddScreenProps) {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const insets = useSafeAreaInsets();
  const { siteId } = route.params;
  const { sites, getUserSites } = useSitesStore();
  
  const [isQrScannerVisible, setIsQrScannerVisible] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const currentSite = sites.find(site => site.id === siteId);

  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DeviceAddForm>({
    resolver: zodResolver(deviceAddSchema),
    defaultValues: {
      setupName: currentSite?.name || '',
      deviceSerialNumber: '',
    },
  });

  const watchedSerialNumber = watch('deviceSerialNumber');

  const handleQrScan = useCallback(
    (code: string) => {
      setValue('deviceSerialNumber', code);
      setIsQrScannerVisible(false);
    },
    [setValue],
  );

  const onSubmit = useCallback(
    async (values: DeviceAddForm) => {
      setIsSubmitting(true);
      try {
        await setupsService.create({
          siteId,
          name: values.setupName,
          deviceQRCode: null,
          deviceSerialNumber: values.deviceSerialNumber,
        });

        // Refresh sites to update hasDevice status
        await getUserSites();

        showToast({
          type: 'success',
          title: t('deviceAdd.success.title'),
          message: t('deviceAdd.success.message'),
          duration: 3000,
        });

        navigation.goBack();
      } catch (error: any) {showToast({
          type: 'error',
          title: t('deviceAdd.error.title'),
          message: error.message || t('deviceAdd.error.message'),
          duration: 5000,
        });
      } finally {
        setIsSubmitting(false);
      }
    },
    [siteId, getUserSites, navigation, t],
  );

  const styles = {
    container: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },
    scrollContainer: {
      flexGrow: 1,
      padding: SPACING.lg,
    },
    mainCard: {
      backgroundColor: COLORS.surface,
      borderRadius: 20,
      padding: SPACING.xl,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 4 },
      shadowOpacity: 0.1,
      shadowRadius: 12,
      elevation: 8,
    },
    header: {
      flexDirection: 'row' as const,
      alignItems: 'center' as const,
      marginBottom: SPACING.lg,
    },
    headerContent: {
      flex: 1,
      marginLeft: SPACING.md,
    },
    title: {
      fontSize: 24,
      fontWeight: '700' as const,
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },
    subtitle: {
      fontSize: 14,
      color: COLORS.textSoft,
      lineHeight: 20,
    },
    siteInfo: {
      backgroundColor: COLORS.accent + '10',
      borderRadius: 12,
      padding: SPACING.lg,
      marginBottom: SPACING.xl,
      borderWidth: 1,
      borderColor: COLORS.accent + '30',
    },
    siteHeader: {
      flexDirection: 'row' as const,
      alignItems: 'center' as const,
      marginBottom: SPACING.sm,
    },
    siteIcon: {
      width: 32,
      height: 32,
      borderRadius: 16,
      backgroundColor: COLORS.accent + '20',
      justifyContent: 'center' as const,
      alignItems: 'center' as const,
      marginRight: SPACING.sm,
    },
    siteName: {
      fontSize: 16,
      fontWeight: '600' as const,
      color: COLORS.textSoft,
      flex: 1,
    },
    siteAddress: {
      fontSize: 12,
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },
    deviceStatus: {
      backgroundColor: COLORS.warning + '20',
      borderRadius: 12,
      paddingHorizontal: SPACING.sm,
      paddingVertical: SPACING.xs,
      alignSelf: 'flex-start' as const,
    },
    deviceStatusText: {
      fontSize: 11,
      fontWeight: '600',
      color: COLORS.warning,
      textTransform: 'uppercase' as const,
    },
    formWrapper: {
      gap: SPACING.lg,
    },
    buttonContainer: {
      marginTop: SPACING.xl,
      gap: SPACING.md,
    },
  };

  return (
    <SafeAreaView style={[styles.container, { backgroundColor: COLORS.dark }]}>
      <KeyboardAvoidingView
        style={{ flex: 1 }}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      >
        <TouchableWithoutFeedback onPress={Keyboard.dismiss}>
          <ScrollView
            style={{ flex: 1 }}
            contentContainerStyle={[
              styles.scrollContainer,
              { paddingTop: insets.top + SPACING.md }
            ]}
            showsVerticalScrollIndicator={false}
          >
            <Card variant="elevated" style={styles.mainCard}>
              {/* Header */}
              <View style={styles.header}>
                <View style={styles.headerContent}>
                  <Text style={styles.title}>{t('deviceAdd.title')}</Text>
                  <Text style={styles.subtitle}>{t('deviceAdd.subtitle')}</Text>
                </View>
              </View>

            {/* Site Info */}
            {currentSite && (
              <View style={styles.siteInfo}>
                <View style={styles.siteHeader}>
                  <View style={styles.siteIcon}>
                    <MaterialIcons name="home" size={18} color={COLORS.accent} />
                  </View>
                  <Text style={styles.siteName}>{currentSite.name}</Text>
                </View>
                <Text style={styles.siteAddress}>
                  {currentSite.city && currentSite.country 
                    ? `${currentSite.city}, ${currentSite.country}`
                    : t('deviceAdd.noAddress')
                  }
                </Text>
              </View>
            )}

            <View style={styles.formWrapper}>
              <Controller
                control={control}
                name="setupName"
                render={({ field }) => (
                  <Input
                    label={t('deviceAdd.setupName')}
                    placeholder={t('deviceAdd.setupNamePlaceholder')}
                    autoCapitalize="words"
                    value={field.value}
                    onChangeText={field.onChange}
                    onBlur={field.onBlur}
                    error={errors.setupName?.message}
                  />
                )}
              />

              <Controller
                control={control}
                name="deviceSerialNumber"
                render={({ field }) => (
                  <Input
                    label={t('deviceAdd.serialNumber')}
                    placeholder={t('deviceAdd.serialNumberPlaceholder')}
                    autoCapitalize="characters"
                    autoCorrect={false}
                    value={field.value}
                    onChangeText={field.onChange}
                    onBlur={field.onBlur}
                    error={errors.deviceSerialNumber?.message}
                  />
                )}
              />

              <Button
                title={t('deviceAdd.scanQr')}
                variant="secondary"
                onPress={() => setIsQrScannerVisible(true)}
                leftIcon={<MaterialIcons name="qr-code-scanner" size={20} color="inherit" />}
              />
            </View>

            <View style={styles.buttonContainer}>
              <Button
                title={isSubmitting ? t('common.loading') : t('deviceAdd.addDevice')}
                onPress={handleSubmit(onSubmit)}
                disabled={isSubmitting || !watchedSerialNumber}
              />
              
              <Button
                title={t('common.back') || 'İptal'}
                variant="secondary"
                onPress={() => navigation.goBack()}
                disabled={isSubmitting}
              />
            </View>
          </Card>
        </ScrollView>
      </TouchableWithoutFeedback>
      </KeyboardAvoidingView>

      <QrScannerModal
        visible={isQrScannerVisible}
        onClose={() => setIsQrScannerVisible(false)}
        onScan={handleQrScan}
      />
    </SafeAreaView>
  );
}
